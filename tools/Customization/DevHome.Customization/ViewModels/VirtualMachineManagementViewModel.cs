// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using DevHome.Customization.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualMachineManagementViewModel));

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly StringResource _commonStringResource;

    private StackedNotificationsBehavior? _notificationsHelper;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    [ObservableProperty]
    private bool _showFullPageProgressRing = true;

    [ObservableProperty]
    private bool _showFeaturesList;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public bool ChangesCanBeApplied => !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => !LoadFeaturesCommand.IsRunning && Features.Any(f => f.HasChanged);

    public VirtualMachineManagementViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
        ];

        _commonStringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    OnPropertyChanged(nameof(HasFeatureChanges));
                });
            }
        };

        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        ApplyChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ApplyChangesCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(ChangesCanBeApplied)));
            }
        };

        _ = LoadFeaturesCommand.ExecuteAsync(null);
    }

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = notificationQueue;

        if (!_isUserAdministrator)
        {
            _dispatcherQueue.EnqueueAsync(ShowNonAdminUserNotification);
        }
    }

    private void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptionalFeatureState.IsEnabled))
        {
            OnPropertyChanged(nameof(HasFeatureChanges));
        }
    }

    private async Task LoadFeaturesAsync()
    {
        await Task.Run(async () =>
        {
            foreach (var featureName in WindowsOptionalFeatureNames.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    var existingFeatureState = Features.FirstOrDefault(f => f.Feature.FeatureName == featureName);
                    if (existingFeatureState != null)
                    {
                        // Update the properties of the existing feature, removing the feature if it is no longer available.
                        if (feature == null || !feature.IsAvailable)
                        {
                            Features.Remove(existingFeatureState);
                        }
                        else
                        {
                            existingFeatureState.Feature = feature;
                        }
                    }
                    else if (feature != null)
                    {
                        // Add the feature if it is available.
                        var featureState = new OptionalFeatureState(feature, _isUserAdministrator, ApplyChangesCommand);
                        featureState.PropertyChanged += FeatureState_PropertyChanged;

                        if (featureState.Feature != null && featureState.Feature.IsAvailable)
                        {
                            Features.Add(featureState);
                        }
                    }
                });
            }

            // Update the UI to show the features list and hide the progress ring. Note that the progress ring
            // is only shown when the page is first loaded.
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                ShowFullPageProgressRing = false;
                ShowFeaturesList = true;
            });
        });
    }

    private async Task ApplyChangesAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await ModifyFeatures();
            await LoadFeaturesCommand.ExecuteAsync(null);
        });
    }

    private void ShowRestartNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            "Changes applied",
            _commonStringResource.GetLocalized("RestartAfterChangesMessage"),
            InfoBarSeverity.Informational,
            RestartComputerCommand,
            _commonStringResource.GetLocalized("RestartButton"));
    }

    private void ShowNonAdminUserNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            "Current user is not an administrator",
            "Only users with the Administrator role can modify optional features.",
            InfoBarSeverity.Informational);
    }

    private void ShowChangesNotAppliedNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            "Changes not applied",
            "Changes were not applied. Please try again.",
            InfoBarSeverity.Warning);
    }

    private void ShowFailedToApplyAllNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            "Failed to apply all changes",
            "Restart the computer and try again.",
            InfoBarSeverity.Error,
            RestartComputerCommand,
            _commonStringResource.GetLocalized("RestartButton"));
    }

    [RelayCommand]
    private void RestartComputer()
    {
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,

            // Restart the computer
            FileName = Environment.SystemDirectory + "\\shutdown.exe",
            Arguments = "-r -t 0",
            Verb = string.Empty,
        };

        var process = new Process
        {
            StartInfo = startInfo,
        };
        process.Start();
    }

    private async Task ModifyFeatures()
    {
        if (!HasFeatureChanges)
        {
            return;
        }

        var featuresString = string.Empty;

        foreach (var featureState in Features)
        {
            if (featureState.HasChanged)
            {
                featuresString += $"{featureState.Feature.FeatureName}={featureState.IsEnabled}`n";
            }
        }

        var startInfo = new ProcessStartInfo();

        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = $"powershell.exe";
        startInfo.Arguments = $"-ExecutionPolicy Bypass -Command \"{ModifyWindowsOptionalFeatures.ModifyFunction.Replace("$args[0]", $"\"{featuresString}\"")}\"";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;
        await Task.Run(() =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception
            try
            {
                process.Start();
                process.WaitForExit();

                _notificationsHelper?.ClearWithWindowExtension();
                _log.Information($"Script exited with code: '{process.ExitCode}'");

                // ExitCodes come directly from within the script in HyperVSetupScript.SetupFunction.
                switch (process.ExitCode)
                {
                    case 0:
                        // The script successfully modified all features
                        ShowRestartNotification();
                        return Task.CompletedTask;
                    case 1:
                        // The script found that nothing needed to be done. The features will be reloaded
                        // to show the correct state.
                        return Task.CompletedTask;
                    case 2:
                    default:
                        // Script failed to modify features, TODO: Show error dialog
                        ShowFailedToApplyAllNotification();
                        return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt. TODO: Show error dialog
                _log.Error(ex, "Script failed");
                ShowChangesNotAppliedNotification();
            }

            return Task.CompletedTask;
        });
    }
}
