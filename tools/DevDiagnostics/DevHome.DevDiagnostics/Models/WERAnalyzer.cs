// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace DevHome.DevDiagnostics.Models;

// This class monitors for WER reports and runs analysis on them
public class WERAnalyzer : IDisposable
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(WERAnalyzer));
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly WERHelper _werHelper;

    private readonly BlockingCollection<WERReport> _analysisRequests = new();

    private readonly ObservableCollection<WERReport> _werReports = [];

    public ReadOnlyObservableCollection<WERReport> WERReports { get; private set; }

    public WERAnalyzer()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Debug.Assert(_dispatcher is not null, "Need to create this object on the UI thread");

        WERReports = new(_werReports);

        _werHelper = Application.Current.GetService<WERHelper>();
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WER_CollectionChanged;

        PopulateCurrentLogs();

        // Create a dedicated thread to serially perform all of our crash dump analysis
        var crashDumpAnalyzerThread = new Thread(() =>
        {
            while (!_analysisRequests.IsCompleted)
            {
                if (_analysisRequests.TryTake(out var report, Timeout.Infinite))
                {
                    PerformAnalysis(report);
                }
            }
        });
        crashDumpAnalyzerThread.Name = "CrashDumpAnalyzerThread";
        crashDumpAnalyzerThread.Start();
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            ProcessDumpList(e.NewItems.Cast<WERBasicReport>().ToList());
        }
    }

    private void PopulateCurrentLogs()
    {
        ProcessDumpList(_werHelper.WERReports.ToList<WERBasicReport>());
    }

    private void ProcessDumpList(List<WERBasicReport> reports)
    {
        List<WERReport> reportsToAnalyze = new();

        // First publish all of these basic reports to our listeners. Then we'll go back and perform
        // analysis on them.
        foreach (var basicReport in reports)
        {
            var reportAnalysis = new WERReport(basicReport);

            _werReports.Add(reportAnalysis);
            reportsToAnalyze.Add(reportAnalysis);

            // When the crash dump path changes, we'll want to perform analysis on it.
            basicReport.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(WERBasicReport.CrashDumpPath))
                {
                    RunToolAnalysis(reportAnalysis);
                }
            };
        }

        foreach (var reportAnalysis in reportsToAnalyze)
        {
            RunToolAnalysis(reportAnalysis);
        }
    }

    private void RunToolAnalysis(WERReport report)
    {
        if (string.IsNullOrEmpty(report.BasicReport.CrashDumpPath))
        {
            // We need a crash dump to perform an analysis
            return;
        }

        // Queue the request that will be processed on a separate thread
        _analysisRequests.Add(report);
    }

    public void Dispose()
    {
        _analysisRequests.CompleteAdding();
        _analysisRequests.Dispose();
        GC.SuppressFinalize(this);
    }

    private uint ProcThreadAttributeValue(int number, bool thread, bool input, bool additive)
    {
        return (uint)(number & 0x0000FFFF | // PROC_THREAD_ATTRIBUTE_NUMBER
                     (thread ? 0x00010000 : 0) | // PROC_THREAD_ATTRIBUTE_THREAD
                     (input ? 0x00020000 : 0) | // PROC_THREAD_ATTRIBUTE_INPUT
                     (additive ? 0x00040000 : 0)); // PROC_THREAD_ATTRIBUTE_ADDITIVE
    }

    public unsafe void PerformAnalysis(WERReport report)
    {
        // See if we have a cached analysis
        var analysisFilePath = GetCachedResultsFileName(report);

        if (File.Exists(analysisFilePath))
        {
            string analysis = File.ReadAllText(analysisFilePath);

            _dispatcher.TryEnqueue(() =>
            {
                report.SetAnalysis(analysis);
            });
        }
        else
        {
            // Generate the analysis
            try
            {
                LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList = default(LPPROC_THREAD_ATTRIBUTE_LIST);
                nuint size = 0;

                if (!PInvoke.InitializeProcThreadAttributeList(lpAttributeList, 2, ref size))
                {
                    throw new InvalidOperationException();
                }

                lpAttributeList = new LPPROC_THREAD_ATTRIBUTE_LIST((void*)Marshal.AllocHGlobal((int)size));

                if (!PInvoke.InitializeProcThreadAttributeList(lpAttributeList, 2, ref size))
                {
                    throw new InvalidOperationException();
                }

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
                uint PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES = ProcThreadAttributeValue(9, false, true, false); // 9 - ProcThreadAttributeSecurityCapabilities
                uint PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY = ProcThreadAttributeValue(15, false, true, false); // 15 - ProcThreadAttributeAllApplicationPackagesPolicy
#pragma warning restore SA1312 // Variable names should begin with lower-case letter

                SID_AND_ATTRIBUTES[] sidAndAttributes = new SID_AND_ATTRIBUTES[1];

                PInvoke.UpdateProcThreadAttribute(lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES, (void*)&sidAndAttributes, 1, null, (nuint*)null);

                // Add LPAC
                uint allApplicationPackagesPolicy = 1; //  PROCESS_CREATION_ALL_APPLICATION_PACKAGES_OPT_OUT;
                PInvoke.UpdateProcThreadAttribute(lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY, &allApplicationPackagesPolicy, sizeof(uint), null, (nuint*)null);
                FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);

                var startInfo = new ProcessStartInfo()
                {
                    FileName = "DumpAnalyzer.exe",
                    Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", report.BasicReport.CrashDumpPath, analysisFilePath),
                    UseShellExecute = true,
                    WorkingDirectory = fileInfo.DirectoryName,
                };

                var process = Process.Start(startInfo);
                Debug.Assert(process != null, "If process launch fails, Process.Start should throw an exception");

                process.WaitForExit();

                if (File.Exists(analysisFilePath))
                {
                    string analysis = File.ReadAllText(analysisFilePath);

                    _dispatcher.TryEnqueue(() =>
                    {
                        report.SetAnalysis(analysis);
                    });
                }
                else
                {
                    // Our analysis failed to work. Log the error
                    _log.Error("Error Analyzing " + report.BasicReport.CrashDumpPath);

                    if (File.Exists(analysisFilePath + ".err"))
                    {
                        _log.Error(File.ReadAllText(analysisFilePath + ".err"));
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
    }

    private string GetCachedResultsFileName(WERReport report)
    {
        return report.BasicReport.CrashDumpPath + ".analysisresults.xml";
    }

    public void RemoveCachedResults(WERReport report)
    {
        var analysisFilePath = GetCachedResultsFileName(report);

        if (File.Exists(analysisFilePath))
        {
            try
            {
                File.Delete(analysisFilePath);
            }
            catch (Exception ex)
            {
                _log.Warning("Failed to delete cache analysis results - " + ex.ToString());
            }
        }
    }
}
