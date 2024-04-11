// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Extensions;
using DevHome.Common.Windows.FileDialog;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;
using WinUIEx;

namespace DevHome.QuietBackgroundProcesses.UI.ViewModels;

public partial class AnalyticSummaryPopupViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AnalyticSummaryPopupViewModel));
    private readonly List<ProcessData> _processDatas = new();
    private readonly List<DevHome.QuietBackgroundProcesses.ProcessRow> _processDatas2 = new();
    private readonly WindowEx _mainWindow;

    public int SortComboBoxIndex { get; set; }

    public AdvancedCollectionView ProcessDatasAd { get; private set; }

    private ProcessData.ProcessCategory ConvertProcessType(DevHome.QuietBackgroundProcesses.ProcessCategory inputType)
    {
        return (ProcessData.ProcessCategory)inputType;
    }

    public AnalyticSummaryPopupViewModel(QuietBackgroundProcesses.ProcessPerformanceTable? performanceTable)
    {
        TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_AnalyticSummary_Open", LogLevel.Info, new QuietBackgroundProcessesEvent());

        _mainWindow = Application.Current.GetService<WindowEx>();

        try
        {
            if (performanceTable != null)
            {
                var rows = performanceTable.Rows;
                foreach (var row in rows)
                {
                    if (row != null)
                    {
                        var sampleCount = row.SampleCount;
                        var sampleDuration = 1;

                        var entry = new ProcessData
                        {
                            Pid = row.Pid,
                            Name = row.Name,
                            PackageFullName = row.PackageFullName,
                            Aumid = row.Aumid,
                            Path = row.Path,
                            Category = ConvertProcessType(row.Category),
                            CreateTime = row.CreateTime,
                            ExitTime = row.ExitTime,
                            Samples = row.SampleCount,
                            Percent = row.PercentCumulative / sampleCount,
                            StandardDeviation = (float)Math.Sqrt(row.VarianceCumulative / sampleCount),
                            Sigma4Deviation = (float)Math.Sqrt(Math.Sqrt(row.Sigma4Cumulative / sampleCount)),
                            MaxPercent = row.MaxPercent,
                            TimeAboveThreshold = TimeSpan.FromSeconds(row.SamplesAboveThreshold * sampleDuration),
                            TotalCpuTimeInMicroseconds = row.TotalCpuTimeInMicroseconds,
                        };

                        entry.TimeAboveThresholdInMinutes = entry.TimeAboveThreshold.TotalMinutes;
                        _processDatas.Add(entry);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error("Error populating performance summary table", ex);
        }

        ProcessDatasAd = new AdvancedCollectionView(_processDatas, true);
        ProcessDatasAd.SortDescriptions.Add(new SortDescription("Pid", SortDirection.Descending));
    }

    [RelayCommand]
    public void FilterProcessesTextInputChanged(string filterExpression)
    {
        ProcessDatasAd.Filter = item =>
        {
            try
            {
                if (item is DevHome.QuietBackgroundProcesses.UI.ProcessData process)
                {
                    return
                        process.Name.Contains(filterExpression, StringComparison.OrdinalIgnoreCase)
                        || process.Category.ToString().Contains(filterExpression, StringComparison.OrdinalIgnoreCase)
                        || process.TimeAboveThreshold.Minutes.ToString(CultureInfo.InvariantCulture).Contains(filterExpression, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception ex)
            {
                _log.Error("Filtering failed", ex);
            }

            return true;
        };

        ProcessDatasAd.RefreshFilter();
    }

    [RelayCommand]
    public void SortProcessesComboBoxChanged(string selectedValue)
    {
        ProcessDatasAd.SortDescriptions.Clear();
        if (selectedValue == "Process")
        {
            ProcessDatasAd.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
        }
        else if (selectedValue == "Type")
        {
            ProcessDatasAd.SortDescriptions.Add(new SortDescription("Category", SortDirection.Descending));
        }
        else if (selectedValue == "CPU above threshold")
        {
            ProcessDatasAd.SortDescriptions.Add(new SortDescription("TimeAboveThreshold", SortDirection.Descending));
        }
    }

    [RelayCommand]
    public void SaveReportButtonClicked()
    {
        using var fileDialog = new WindowSaveFileDialog();
        fileDialog.AddFileType("CSV files", ".csv");

        var filePath = fileDialog.Show(_mainWindow);
        if (filePath == null)
        {
            return;
        }

        // Save the report to a .csv
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the .csv header
            writer.WriteLine("Pid,Name,Samples,Percent,StandardDeviation,Sigma4Deviation,MaxPercent,TimeAboveThreshold,TotalCpuTimeInMicroseconds,PackageFullName,Aumid,Path,Category,CreateTime,ExitTime");

            // Write each item from the list to the file
            foreach (var data in this._processDatas)
            {
                string row = $"{data.Pid},{data.Name},{data.Samples},{data.Percent},{data.StandardDeviation},{data.Sigma4Deviation},{data.MaxPercent},{data.TimeAboveThreshold},{data.TotalCpuTimeInMicroseconds},{data.PackageFullName},{data.Aumid},{data.Path},{data.Category},{data.CreateTime},{data.ExitTime}";
                writer.WriteLine(row);
            }
        }
    }
}
