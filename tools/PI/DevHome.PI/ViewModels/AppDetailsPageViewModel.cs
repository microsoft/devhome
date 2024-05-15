// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Win32;

namespace DevHome.PI.ViewModels;

public partial class AppDetailsPageViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppDetailsPageViewModel));

    [ObservableProperty]
    private AppRuntimeInfo appInfo;

    [ObservableProperty]
    private Visibility runAsAdminVisibility = Visibility.Collapsed;

    private Process? targetProcess;

    public AppDetailsPageViewModel()
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        AppInfo = new();

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
            RunAsAdminVisibility = Visibility.Collapsed;
            AppInfo = new();

            try
            {
                AppInfo.ProcessId = targetProcess.Id;
                AppInfo.IsRunningAsSystem = TargetAppData.Instance.IsRunningAsSystem;

                if (!process.HasExited)
                {
                    AppInfo.BasePriority = targetProcess.BasePriority;
                    AppInfo.IsRunningAsAdmin = TargetAppData.Instance.IsRunningAsAdmin;
                    AppInfo.Visibility = Visibility.Visible;
                    AppInfo.PriorityClass = (int)targetProcess.PriorityClass;

                    if (targetProcess.MainModule != null)
                    {
                        AppInfo.MainModuleFileName = targetProcess.MainModule.FileName;
                        uint binaryTypeValue;
                        PInvoke.GetBinaryType(AppInfo.MainModuleFileName, out binaryTypeValue);
                        AppInfo.BinaryType = (WindowHelper.BinaryType)binaryTypeValue;
                    }

                    foreach (ProcessModule module in targetProcess.Modules)
                    {
                        if (module.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesWpf = true;
                        }

                        if (module.ModuleName.Equals("System.Windows.Forms.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesWinForms = true;
                        }

                        if (module.ModuleName.Contains("mfc", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesMfc = true;
                        }

                        if (module.ModuleName.Contains("Avalonia", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.IsAvalonia = true;
                        }

                        if (module.ModuleName.Contains("Microsoft.Maui", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.IsMaui = true;
                        }

                        if (module.ModuleName.Contains("Microsoft.Windows.SDK.NET", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesWinAppSdk = true;
                        }

                        if (module.ModuleName.Contains("Microsoft.WinUI", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesWinUi = true;
                        }

                        if (module.ModuleName.Contains("dxcore", StringComparison.OrdinalIgnoreCase))
                        {
                            AppInfo.UsesDirectX = true;
                        }
                    }

                    AppInfo.IsStoreApp = PInvoke.IsImmersiveProcess(targetProcess.SafeHandle);
                }
            }
            catch (Win32Exception ex)
            {
                // This can throw if the process is running elevated and we are not.
                _log.Error(ex, "Unable to contruct an AppInfo for target process.");
                if (ex.NativeErrorCode == (int)Windows.Win32.Foundation.WIN32_ERROR.ERROR_ACCESS_DENIED)
                {
                    // Hide properties that cannot be retrieved when the target app is elevated and PI is not.
                    AppInfo.Visibility = Visibility.Collapsed;

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
            CommonHelper.RunAsAdmin(targetProcess.Id, nameof(AppDetailsPageViewModel));
        }
    }
}
