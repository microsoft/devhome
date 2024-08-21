// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.DevInsights.Models;
using DevHome.DevInsights.Properties;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.ViewModels;

public partial class ProcessListPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private string filterProcessText;

    partial void OnFilterProcessTextChanged(string value)
    {
        FilterProcessList();
    }

    [ObservableProperty]
    private ObservableCollection<Process> processes;

    [ObservableProperty]
    private ObservableCollection<Process> filteredProcesses;

    public ProcessListPageViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        processes = new();
        filteredProcesses = new();
        filterProcessText = string.Empty;
        GetFilteredProcessList();

        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
    }

    public void ResetPage()
    {
        FilterProcessText = string.Empty;
        GetFilteredProcessList();
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if ((e.PropertyName == nameof(TargetAppData.TargetProcess)) || (e.PropertyName == nameof(TargetAppData.HasExited)))
        {
            GetFilteredProcessList();
        }
    }

    private void GetFilteredProcessList()
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            var currentProcesses = Process.GetProcesses();
            UpdateFilteredProcessList(currentProcesses);
        });
    }

    private void UpdateFilteredProcessList(Process[] currentProcesses)
    {
        dispatcher.TryEnqueue(() =>
        {
            Processes.Clear();
            var sortedProcesses = currentProcesses.OrderBy(process => process.ProcessName).ToArray();
            foreach (var proc in sortedProcesses)
            {
                var settings = Settings.Default;
                if (string.Equals(proc.ProcessName, "backgroundtaskhost", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeBgTaskHost)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "conhost", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeConHost)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "dllhost", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeDllHost)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "svchost", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeSvcHost)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "msedgewebview2", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeWebview)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "runtimebroker", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeRtb)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "wmiprvse", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeWmi)
                    {
                        Processes.Add(proc);
                    }
                }
                else if (string.Equals(proc.ProcessName, "wudfhost", StringComparison.OrdinalIgnoreCase))
                {
                    if (settings.IsProcessFilterIncludeWudf)
                    {
                        Processes.Add(proc);
                    }
                }
                else
                {
                    Processes.Add(proc);
                }
            }

            foreach (var currentProcess in currentProcesses)
            {
                if (!Processes.Contains(currentProcess))
                {
                    currentProcess.Dispose();
                }
            }

            FilterProcessList();
        });
    }

    public void ProcessDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems != null && e.AddedItems.Count > 0)
        {
            if (e.AddedItems[0] is Process selectedProcess)
            {
                TargetAppData.Instance.SetNewAppData(selectedProcess, (Windows.Win32.Foundation.HWND)selectedProcess.MainWindowHandle);
            }
        }
    }

    public void FilterDropDownClosed()
    {
        Settings.Default.Save();
        GetFilteredProcessList();
    }

    private void FilterProcessList()
    {
        FilteredProcesses = new ObservableCollection<Process>(Processes.Where(
            item =>
            {
                return item.ProcessName.Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase) ||
                Convert.ToString(item.Id, CultureInfo.CurrentCulture).Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase);
            }));
    }

    [RelayCommand]
    private void RefreshFilteredProcessList()
    {
        GetFilteredProcessList();
    }
}
