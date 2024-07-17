// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.ApplicationModel;
using Windows.System.Diagnostics;

namespace DevHome.PI.ViewModels;

public partial class AppDetailsPageViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppDetailsPageViewModel));

    private readonly string _noIssuesText = CommonHelper.GetLocalizedString("NoIssuesText");

    [ObservableProperty]
    private AppRuntimeInfo _appInfo;

    [ObservableProperty]
    private Visibility _runAsAdminVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _processRunningParamsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _processPackageVisibility = Visibility.Collapsed;

    private Process? _targetProcess;

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
        if (_targetProcess != process)
        {
            _targetProcess = process;
            RunAsAdminVisibility = Visibility.Collapsed;
            AppInfo = new();

            try
            {
                AppInfo.ProcessId = _targetProcess.Id;

                if (process.HasExited)
                {
                    AppInfo.Visibility = Visibility.Collapsed;
                    ProcessRunningParamsVisibility = Visibility.Collapsed;
                }
                else
                {
                    AppInfo.Visibility = Visibility.Visible;
                    ProcessRunningParamsVisibility = Visibility.Visible;
                    AppInfo.IsRunningAsSystem = TargetAppData.Instance.IsRunningAsSystem;
                    AppInfo.IsRunningAsAdmin = TargetAppData.Instance.IsRunningAsAdmin;
                    AppInfo.BasePriority = _targetProcess.BasePriority;
                    AppInfo.PriorityClass = (int)_targetProcess.PriorityClass;

                    if (_targetProcess.MainModule != null)
                    {
                        AppInfo.MainModuleFileName = _targetProcess.MainModule.FileName;
                        var cpuArchitecture = WindowHelper.GetAppArchitecture(
                            _targetProcess.SafeHandle, _targetProcess.MainModule.FileName);
                        AppInfo.CpuArchitecture = cpuArchitecture;
                    }

                    AppInfo.CheckFrameworksAndCommandLine(_targetProcess);
                    var pdi = ProcessDiagnosticInfo.TryGetForProcessId((uint)(_targetProcess?.Id ?? 0));
                    if (pdi is not null)
                    {
                        GetPackageInfo(pdi);
                    }
                }
            }
            catch (Win32Exception ex)
            {
                // This can throw if the process is running elevated and we are not.
                _log.Error(ex, "Unable to construct an AppInfo for target process.");
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
            catch (Exception e)
            {
                _log.Error(e, "Failed to update target process.");
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
        if (_targetProcess is not null)
        {
            CommonHelper.RunAsAdmin(_targetProcess.Id, nameof(AppDetailsPageViewModel));
        }
    }

    private void GetPackageInfo(ProcessDiagnosticInfo pdi)
    {
        if (pdi.IsPackaged)
        {
            AppInfo.IsPackaged = true;
            ProcessPackageVisibility = Visibility.Visible;

            var package = pdi.GetAppDiagnosticInfos().FirstOrDefault()?.AppInfo.Package;
            if (package is not null)
            {
                if (package.Id is not null)
                {
                    AppInfo.PackageInfo.FullName = package.Id.FullName;
                    var version = package.Id.Version;
                    AppInfo.PackageInfo.Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                }

                AppInfo.PackageInfo.DisplayName = package.DisplayName;
                AppInfo.PackageInfo.InstalledDate = package.InstalledDate.ToString(CultureInfo.CurrentCulture);
                AppInfo.PackageInfo.InstalledPath = package.InstalledPath;
                AppInfo.PackageInfo.Publisher = package.PublisherDisplayName;
                AppInfo.PackageInfo.IsDevelopmentMode = package.IsDevelopmentMode;
                AppInfo.PackageInfo.SignatureKind = $"{package.SignatureKind}";
                AppInfo.PackageInfo.Status = GetPackageStatus(package);

                List<string> dependencies = [];
                foreach (var d in package.Dependencies)
                {
                    dependencies.Add(d.Id.FullName);
                }

                AppInfo.PackageInfo.Dependencies = string.Join(", ", dependencies);
            }
        }
        else
        {
            ProcessPackageVisibility = Visibility.Collapsed;
        }
    }

    private string GetPackageStatus(Package p)
    {
        // Convert the individual bool Status properties to a list of matching strings.
        List<string> trueProperties = [];
        var status = p.Status;
        string combinedStatus;

        if (status.DataOffline)
        {
            trueProperties.Add(nameof(status.DataOffline));
        }

        if (status.DependencyIssue)
        {
            trueProperties.Add(nameof(status.DependencyIssue));
        }

        if (status.DeploymentInProgress)
        {
            trueProperties.Add(nameof(status.DeploymentInProgress));
        }

        if (status.Disabled)
        {
            trueProperties.Add(nameof(status.Disabled));
        }

        if (status.IsPartiallyStaged)
        {
            trueProperties.Add(nameof(status.IsPartiallyStaged));
        }

        if (status.LicenseIssue)
        {
            trueProperties.Add(nameof(status.LicenseIssue));
        }

        if (status.Modified)
        {
            trueProperties.Add(nameof(status.Modified));
        }

        if (status.NeedsRemediation)
        {
            trueProperties.Add(nameof(status.NeedsRemediation));
        }

        if (status.NotAvailable)
        {
            trueProperties.Add(nameof(status.NotAvailable));
        }

        if (status.PackageOffline)
        {
            trueProperties.Add(nameof(status.PackageOffline));
        }

        if (status.Servicing)
        {
            trueProperties.Add(nameof(status.Servicing));
        }

        if (status.Tampered)
        {
            trueProperties.Add(nameof(status.Tampered));
        }

        if (trueProperties.Count > 0)
        {
            combinedStatus = string.Join(", ", trueProperties);
        }
        else
        {
            combinedStatus = _noIssuesText;
        }

        return combinedStatus;
    }
}
