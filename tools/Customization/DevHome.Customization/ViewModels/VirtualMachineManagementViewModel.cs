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
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using DevHome.Customization.Helpers;
using DevHome.Customization.Models;
using Microsoft.UI.Dispatching;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualMachineManagementViewModel));

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    private readonly DispatcherQueue _dispatcherQueue;

    private OptionalFeatureNotificationHelper? _notificationsHelper;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public bool ChangesCanBeApplied => !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => FeaturesLoaded && Features.Any(f => f.HasChanged);

    public VirtualMachineManagementViewModel(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
        ];

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    OnPropertyChanged(nameof(FeaturesLoaded));
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
        _notificationsHelper = new(notificationQueue, _log);

        if (!_isUserAdministrator)
        {
            _dispatcherQueue.EnqueueAsync(_notificationsHelper.ShowNonAdminUserNotification);
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
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                Features.Clear();
            });

            foreach (var featureName in WindowsOptionalFeatureNames.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                if (feature != null && feature.IsAvailable)
                {
                    var featureState = new OptionalFeatureState(feature, _isUserAdministrator, ApplyChangesCommand);
                    featureState.PropertyChanged += FeatureState_PropertyChanged;

                    await _dispatcherQueue.EnqueueAsync(() =>
                    {
                        Features.Add(featureState);
                    });
                }
            }
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
            // This can also be cancelled by the user which will result in an exception,
            // which is handled as a failure.
            var exitCode = ModifyWindowsOptionalFeatures.ExitCode.Failure;

            try
            {
                process.Start();
                process.WaitForExit();

                exitCode = ModifyWindowsOptionalFeatures.FromExitCode(process.ExitCode);
                _notificationsHelper?.HandleModifyFeatureResult(exitCode);
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt.
                _log.Error(ex, "Script failed");
            }

            _notificationsHelper?.HandleModifyFeatureResult(exitCode);

            return Task.CompletedTask;
        });
    }
}
