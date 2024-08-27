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
        ProcessesView.Refresh();
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
        _processesView.SortDescriptions.Add(new SortDescription(nameof(Process.ProcessName), SortDirection.Ascending));
        _processesView.Filter = entry => FilterProcess((Process)entry);
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
            foreach (var proc in currentProcesses)
            {
                Processes.Add(proc);
            }

            ProcessesView.Refresh();
        });
    }

    private bool FilterProcess(Process process)
    {
        bool showProcess;
        var settings = Settings.Default;

        if (string.Equals(process.ProcessName, "backgroundtaskhost", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeBgTaskHost;
        }
        else if (string.Equals(process.ProcessName, "conhost", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeConHost;
        }
        else if (string.Equals(process.ProcessName, "dllhost", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeDllHost;
        }
        else if (string.Equals(process.ProcessName, "svchost", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeSvcHost;
        }
        else if (string.Equals(process.ProcessName, "msedgewebview2", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeWebview;
        }
        else if (string.Equals(process.ProcessName, "runtimebroker", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeRtb;
        }
        else if (string.Equals(process.ProcessName, "wmiprvse", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeWmi;
        }
        else if (string.Equals(process.ProcessName, "wudfhost", StringComparison.OrdinalIgnoreCase))
        {
            showProcess = settings.IsProcessFilterIncludeWudf;
        }
        else
        {
            showProcess = true;
        }

        if (showProcess)
        {
            return process.ProcessName.Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase) ||
                Convert.ToString(process.Id, CultureInfo.CurrentCulture).Contains(FilterProcessText, StringComparison.CurrentCultureIgnoreCase);
        }

        return showProcess;
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

    [RelayCommand]
    private void RefreshFilteredProcessList()
    {
        GetFilteredProcessList();
    }
}
