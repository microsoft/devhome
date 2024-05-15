// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using DevHome.Telemetry;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.QuietBackgroundProcesses.UI.ViewModels;

public partial class AnalyticSummaryPopupViewModel : ObservableObject
{
    // Enum for process category
    public enum ProcessTableColumn
    {
        Process,
        Type,
        CPUAboveThreshold,
    }

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AnalyticSummaryPopupViewModel));
    private readonly List<ProcessData> _processDatas = new();

    public int SortComboBoxIndex { get; set; }

    public AdvancedCollectionView ProcessDatasAd { get; private set; }

    private ProcessData.ProcessCategory ConvertProcessType(DevHome.QuietBackgroundProcesses.ProcessCategory inputType)
    {
        return (ProcessData.ProcessCategory)inputType;
    }

    public AnalyticSummaryPopupViewModel(QuietBackgroundProcesses.ProcessPerformanceTable? performanceTable)
    {
        TelemetryFactory.Get<ITelemetry>().Log("QuietBackgroundProcesses_AnalyticSummary_Open", LogLevel.Info, new QuietBackgroundProcessesEvent());

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

    private ProcessTableColumn GetProcessTableColumnFromString(string value)
    {
        if (string.Equals(value, "Process", StringComparison.Ordinal))
        {
            return ProcessTableColumn.Process;
        }
        else if (string.Equals(value, "Type", StringComparison.Ordinal))
        {
            return ProcessTableColumn.Type;
        }
        else if (string.Equals(value, "CPUAboveThreshold", StringComparison.Ordinal))
        {
            return ProcessTableColumn.CPUAboveThreshold;
        }

        throw new ArgumentException("Invalid value for ProcessTableColumn");
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
    public void SortProcessesComboBoxChanged(string selectedValueString)
    {
        ProcessDatasAd.SortDescriptions.Clear();

        var selectedValue = GetProcessTableColumnFromString(selectedValueString);
        switch (selectedValue)
        {
            case ProcessTableColumn.Process:
                ProcessDatasAd.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
                break;
            case ProcessTableColumn.Type:
                ProcessDatasAd.SortDescriptions.Add(new SortDescription("Category", SortDirection.Descending));
                break;
            case ProcessTableColumn.CPUAboveThreshold:
                ProcessDatasAd.SortDescriptions.Add(new SortDescription("TimeAboveThreshold", SortDirection.Descending));
                break;
        }
    }

    public void SaveReport(string filePath)
    {
        // Save the report to a .csv
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write the .csv header
            writer.WriteLine("Pid," +
                "Name," +
                "Samples," +
                "Percent," +
                "StandardDeviation," +
                "Sigma4Deviation," +
                "MaxPercent," +
                "TimeAboveThreshold," +
                "TotalCpuTimeInMicroseconds," +
                "PackageFullName," +
                "Aumid," +
                "Path," +
                "Category," +
                "CreateTime," +
                "ExitTime");

            // Write each item from the list to the file
            foreach (var data in this._processDatas)
            {
                var row = $"{data.Pid}," +
                    $"{data.Name}," +
                    $"{data.Samples}," +
                    $"{data.Percent}," +
                    $"{data.StandardDeviation}," +
                    $"{data.Sigma4Deviation}," +
                    $"{data.MaxPercent}," +
                    $"{data.TimeAboveThreshold}," +
                    $"{data.TotalCpuTimeInMicroseconds}," +
                    $"{data.PackageFullName}," +
                    $"{data.Aumid}," +
                    $"{data.Path}," +
                    $"{data.Category}," +
                    $"{data.CreateTime}," +
                    $"{data.ExitTime}";
                writer.WriteLine(row);
            }
        }
    }
}
