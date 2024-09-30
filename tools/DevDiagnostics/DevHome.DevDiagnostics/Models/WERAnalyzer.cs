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
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.Win32.SafeHandles;
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

                PInvoke.InitializeProcThreadAttributeList(lpAttributeList, 2, ref size);

                if (size == 0)
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

                SECURITY_CAPABILITIES securityCapabilities = default(SECURITY_CAPABILITIES);
                SID_AND_ATTRIBUTES[] sidAndAttributes = new SID_AND_ATTRIBUTES[6];
                FreeSidSafeHandle currentPackageSid;
                FreeSidSafeHandle lpacAppSid;
                FreeSidSafeHandle lpacComSid;
                FreeSidSafeHandle lpacInstrumentationSid;
                FreeSidSafeHandle registryReadSid;
                FreeSidSafeHandle lpacIdentityServicesSid;
                FreeSidSafeHandle fileSystemSid;

                PInvoke.DeriveAppContainerSidFromAppContainerName("Microsoft.Windows.DevHome_8wekyb3d8bbwe", out currentPackageSid);

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-1065365936-1281604716-3511738428-1654721687-432734479-3232135806-4053264122-3456934681", out registryReadSid);
                sidAndAttributes[0] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[0].Sid = (PSID)registryReadSid.DangerousGetHandle();
                sidAndAttributes[0].Attributes = 4; // SE_GROUP_ENABLED

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-1502825166-1963708345-2616377461-2562897074-4192028372-3968301570-1997628692-1435953622", out lpacAppSid);
                sidAndAttributes[1] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[1].Sid = (PSID)lpacAppSid.DangerousGetHandle();
                sidAndAttributes[1].Attributes = 4; // SE_GROUP_ENABLED

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-2405443489-874036122-4286035555-1823921565-1746547431-2453885448-3625952902-991631256", out lpacComSid);
                sidAndAttributes[2] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[2].Sid = (PSID)lpacComSid.DangerousGetHandle();
                sidAndAttributes[2].Attributes = 4; // SE_GROUP_ENABLED

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-3153509613-960666767-3724611135-2725662640-12138253-543910227-1950414635-4190290187", out lpacInstrumentationSid);
                sidAndAttributes[3] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[3].Sid = (PSID)lpacInstrumentationSid.DangerousGetHandle();
                sidAndAttributes[3].Attributes = 4; // SE_GROUP_ENABLED

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-1788129303-2183208577-3999474272-3147359985-1757322193-3815756386-151582180-1888101193", out lpacIdentityServicesSid);
                sidAndAttributes[4] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[4].Sid = (PSID)lpacIdentityServicesSid.DangerousGetHandle();
                sidAndAttributes[4].Attributes = 4; // SE_GROUP_ENABLED

                PInvoke.ConvertStringSidToSid("S-1-15-3-1024-3777909873-1799880613-452196415-3098254733-3833254313-651931560-4017485463-3376623984", out fileSystemSid);
                sidAndAttributes[5] = default(SID_AND_ATTRIBUTES);
                sidAndAttributes[5].Sid = (PSID)fileSystemSid.DangerousGetHandle();
                sidAndAttributes[5].Attributes = 4; // SE_GROUP_ENABLED

                securityCapabilities.CapabilityCount = 6;
                securityCapabilities.AppContainerSid = (PSID)currentPackageSid.DangerousGetHandle();
                securityCapabilities.Reserved = 0;

                FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);
                PROCESS_INFORMATION pi;

                fixed (SID_AND_ATTRIBUTES* sids = sidAndAttributes)
                {
                    securityCapabilities.Capabilities = sids;
                    bool f = PInvoke.UpdateProcThreadAttribute(lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES, sids, (nuint)sizeof(SECURITY_CAPABILITIES), null, (nuint*)null);

                    // Add LPAC
                    uint allApplicationPackagesPolicy = 1; //  PROCESS_CREATION_ALL_APPLICATION_PACKAGES_OPT_OUT;
                    f = PInvoke.UpdateProcThreadAttribute(lpAttributeList, 0, PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY, &allApplicationPackagesPolicy, sizeof(uint), null, (nuint*)null);

                    string commandLineString = "DumpAnalyzer.exe " + string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"\0", report.BasicReport.CrashDumpPath, analysisFilePath);
                    Span<char> commandLine = new char[commandLineString.Length];
                    commandLineString.TryCopyTo(commandLine);

                    STARTUPINFOEXW startupex = default(STARTUPINFOEXW);
                    startupex.StartupInfo.cb = (uint)sizeof(STARTUPINFOEXW);
                    startupex.lpAttributeList = lpAttributeList;

                    f = CreateProcessEx(
                                          @"D:\devhome\src\bin\x64\Debug\net8.0-windows10.0.22621.0\AppX\DumpAnalyzer.exe",
                                          ref commandLine,
                                          null,
                                          null,
                                          false,
                                          PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
                                          null,
                                          fileInfo.DirectoryName ?? string.Empty,
                                          startupex,
                                          out pi);
                    int lasterror = Marshal.GetLastWin32Error();
                }

                Process p = Process.GetProcessById((int)pi.dwProcessId);

                if (p is null)
                {
                    throw new InvalidProgramException();
                }

                p.WaitForExit();
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

    internal static unsafe BOOL CreateProcessEx(string lpApplicationName, ref Span<char> lpCommandLine, SECURITY_ATTRIBUTES? lpProcessAttributes, SECURITY_ATTRIBUTES? lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, void* lpEnvironment, string lpCurrentDirectory, in STARTUPINFOEXW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation)
    {
        if (lpCommandLine != null && lpCommandLine.LastIndexOf('\0') == -1)
        {
            throw new ArgumentException("Required null terminator missing.", nameof(lpCommandLine));
        }

        fixed (PROCESS_INFORMATION* lpProcessInformationLocal = &lpProcessInformation)
        {
            fixed (STARTUPINFOEXW* lpStartupInfoLocal = &lpStartupInfo)
            {
                fixed (char* lpCurrentDirectoryLocal = lpCurrentDirectory)
                {
                    fixed (char* plpCommandLine = lpCommandLine)
                    {
                        fixed (char* lpApplicationNameLocal = lpApplicationName)
                        {
                            PWSTR wstrlpCommandLine = plpCommandLine;
                            SECURITY_ATTRIBUTES lpProcessAttributesLocal = lpProcessAttributes ?? default(SECURITY_ATTRIBUTES);
                            SECURITY_ATTRIBUTES lpThreadAttributesLocal = lpThreadAttributes ?? default(SECURITY_ATTRIBUTES);
                            BOOL result = CreateProcessEx(lpApplicationNameLocal, wstrlpCommandLine, lpProcessAttributes.HasValue ? &lpProcessAttributesLocal : null, lpThreadAttributes.HasValue ? &lpThreadAttributesLocal : null, bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectoryLocal, lpStartupInfoLocal, lpProcessInformationLocal);
                            lpCommandLine = lpCommandLine.Slice(0, wstrlpCommandLine.Length);
                            return result;
                        }
                    }
                }
            }
        }
    }

    [DllImport("KERNEL32.dll", ExactSpelling = true, EntryPoint = "CreateProcessW", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern unsafe BOOL CreateProcessEx(PCWSTR lpApplicationName, PWSTR lpCommandLine, [Optional] SECURITY_ATTRIBUTES* lpProcessAttributes, [Optional] SECURITY_ATTRIBUTES* lpThreadAttributes, BOOL bInheritHandles, PROCESS_CREATION_FLAGS dwCreationFlags, [Optional] void* lpEnvironment, PCWSTR lpCurrentDirectory, STARTUPINFOEXW* lpStartupInfo, PROCESS_INFORMATION* lpProcessInformation);

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
