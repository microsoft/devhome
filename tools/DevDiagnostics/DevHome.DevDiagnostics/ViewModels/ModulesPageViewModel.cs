// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Models;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class ModulesPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ProcessModuleInfo> moduleList;

    [ObservableProperty]
    private int selectedModuleIndex;

    [ObservableProperty]
    private Visibility runAsAdminVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility gridVisibility = Visibility.Visible;

    private Process? targetProcess;

    public ModulesPageViewModel()
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        moduleList = new();
        selectedModuleIndex = 0;

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (targetProcess != process)
        {
            targetProcess = process;
            ModuleList.Clear();
            GridVisibility = Visibility.Visible;
            RunAsAdminVisibility = Visibility.Collapsed;

            try
            {
                if (!process.HasExited)
                {
                    // Sort the list based on the module name.
                    var moduleList = targetProcess.Modules.Cast<ProcessModule>().ToList();
                    moduleList = [.. moduleList.OrderBy(module => module.ModuleName)];

                    foreach (var module in moduleList)
                    {
                        ModuleList.Add(new ProcessModuleInfo(module));
                    }

                    if (ModuleList.Count > 0)
                    {
                        SelectedModuleIndex = 0;
                    }
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == (int)Windows.Win32.Foundation.WIN32_ERROR.ERROR_ACCESS_DENIED)
                {
                    GridVisibility = Visibility.Collapsed;

                    // Only show the button when not running as admin. This is possible when the target app is a system app.
                    if (!RuntimeHelper.IsCurrentProcessRunningAsAdmin())
                    {
                        RunAsAdminVisibility = Visibility.Visible;
                    }
                }
            }
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            if (TargetAppData.Instance.TargetProcess is not null)
            {
                UpdateTargetProcess(TargetAppData.Instance.TargetProcess);
            }
        }
    }

    [RelayCommand]
    private void RunAsAdmin()
    {
        if (targetProcess is not null)
        {
            CommonHelper.RunAsAdmin(targetProcess.Id, nameof(ModulesPageViewModel));
        }
    }
}
