// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.ViewModels;

public partial class ProcessListPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private ObservableCollection<Process> processes;

    public ProcessListPageViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        processes = new();
        GetFilteredProcessList();
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

    [RelayCommand]
    private void RefreshFilteredProcessList()
    {
        GetFilteredProcessList();
    }
}
