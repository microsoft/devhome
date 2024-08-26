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
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.DevInsights.Models;
using DevHome.DevInsights.Properties;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.ViewModels;

public partial class ProcessListPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private string _filterProcessText;

    partial void OnFilterProcessTextChanged(string value)
    {
        FilterProcessList();
    }

    [ObservableProperty]
    private ObservableCollection<Process> _processes;

    [ObservableProperty]
    private AdvancedCollectionView _processesView;

    public ProcessListPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _processes = new();
        _filterProcessText = string.Empty;
        _processesView = new AdvancedCollectionView(_processes, true);
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
        _dispatcher.TryEnqueue(() =>
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

    public void SortProcesses(object sender, DataGridColumnEventArgs e)
    {
        var propertyName = string.Empty;
        if (e.Column.DisplayIndex == 0)
        {
            propertyName = nameof(Process.Id);
        }
        else if (e.Column.DisplayIndex == 1)
        {
            propertyName = nameof(Process.ProcessName);
        }

        if (!string.IsNullOrEmpty(propertyName))
        {
            if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
            {
                // Clear pervious sorting
                ProcessesView.SortDescriptions.Clear();
                ProcessesView.SortDescriptions.Add(new SortDescription(propertyName, SortDirection.Ascending));
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }
            else
            {
                ProcessesView.SortDescriptions.Clear();
                ProcessesView.SortDescriptions.Add(new SortDescription(propertyName, SortDirection.Descending));
                e.Column.SortDirection = DataGridSortDirection.Descending;
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
        /*FilteredProcesses = new ObservableCollection<Process>(Processes.Where(
            item =>
            {
                return item.ProcessName.Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase) ||
                Convert.ToString(item.Id, CultureInfo.CurrentCulture).Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase);
            }));*/
    }

    [RelayCommand]
    private void RefreshFilteredProcessList()
    {
        GetFilteredProcessList();
    }
}
