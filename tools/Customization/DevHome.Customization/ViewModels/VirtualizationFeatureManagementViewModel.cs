// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
using DevHome.Customization.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualizationFeatureManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualizationFeatureManagementViewModel));

    private readonly StringResource _stringResource;

    private readonly bool _isUserAdministrator = new WindowsIdentityHelper().IsUserAdministrator();

    private readonly Dictionary<string, bool> _initialFeatureEnabledStates = new();

    private readonly Window _window;

    private readonly ModifyFeaturesDialog _modifyFeaturesDialog;

    private StackedNotificationsBehavior? _notificationQueue;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !_isFirstLoad || !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public bool ChangesCanBeApplied => HasFeatureChanges && !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<WindowsOptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => _isUserAdministrator && FeaturesLoaded && Features.Any(f => f.HasChanged);

    public bool CanDismissNotifications => _isUserAdministrator;

    private bool _restartNeeded;

    private bool _isFirstLoad;

    public VirtualizationFeatureManagementViewModel(Window window)
    {
        _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        _window = window;
        _isFirstLoad = true;

        Breadcrumbs =
        [
            new(_stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(_stringResource.GetLocalized("VirtualizationFeatureManagement_Header"), typeof(VirtualizationFeatureManagementViewModel).FullName!)
        ];

        LoadFeaturesCommand = new AsyncRelayCommand(LoadFeaturesAsync);
        LoadFeaturesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoadFeaturesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        ApplyChangesCommand = new AsyncRelayCommand(ApplyChangesAsync);
        ApplyChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(ApplyChangesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        _modifyFeaturesDialog = new ModifyFeaturesDialog(ApplyChangesCommand)
        {
            XamlRoot = _window.Content.XamlRoot,
        };
    }

    internal async Task Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;

        var loadingTask = LoadFeaturesCommand.ExecuteAsync(null);

        if (!_isUserAdministrator)
        {
            await _window.DispatcherQueue.EnqueueAsync(ShowNonAdminUserNotification);
        }

        if (_restartNeeded)
        {
            await _window.DispatcherQueue.EnqueueAsync(ShowRestartNotification);
        }

        await loadingTask;
    }

    internal void Uninitialize()
    {
        _notificationQueue = null;
    }

    private async void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowsOptionalFeatureState.IsEnabled))
        {
            await OnFeaturesChanged();
        }
    }

    private async Task LoadFeaturesAsync()
    {
        var tempFeatures = new ObservableCollection<WindowsOptionalFeatureState>();

        await Task.Run(() =>
        {
            foreach (var featureName in WindowsOptionalFeatures.VirtualMachineFeatures)
            {
                var feature = ManagementInfrastructureHelper.GetWindowsFeatureDetails(featureName);
                if (feature != null && feature.IsAvailable)
                {
                    // A features is consider modifiable if the user is an administrator.
                    var featureState = new WindowsOptionalFeatureState(feature, _isUserAdministrator);
                    featureState.PropertyChanged += FeatureState_PropertyChanged;

                    // Add to the temporary list instead of the main Features collection to avoid the UI updating
                    // for each feature added since GetWindowsFeatureDetails can take some time.
                    tempFeatures.Add(featureState);

                    // Keep track of the original feature state to determine if changes have been made to provide
                    // a notification to the user if they try to navigate away without applying changes or restarting
                    // when needed.
                    if (!_initialFeatureEnabledStates.ContainsKey(featureName))
                    {
                        _initialFeatureEnabledStates.Add(featureName, featureState.IsEnabled);
                    }
                }
            }

            return Task.CompletedTask;
        });

        // Update the Features collection all at once
        await _window.DispatcherQueue.EnqueueAsync(() =>
        {
            Features.Clear();
            foreach (var featureState in tempFeatures)
            {
                Features.Add(featureState);
            }
        });

        // After the first load, set _isFirstLoad to false so that the list is not hidden after the initial
        // load from an empty page. Subsequent loads will keep the list visible if any changes are made.
        if (_isFirstLoad)
        {
            _isFirstLoad = false;
        }

        await OnFeaturesChanged();
    }

    private bool HasFeatureStatusChanged()
    {
        foreach (var feature in Features)
        {
            if (_initialFeatureEnabledStates.TryGetValue(feature.Feature.FeatureName, out var initialState))
            {
                if (initialState != feature.IsEnabled)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task ApplyChangesAsync()
    {
        _notificationQueue?.ClearWithWindowExtension();

        var cancellationTokenSource = new CancellationTokenSource();
        _modifyFeaturesDialog.ViewModel.SetCommittingChanges(cancellationTokenSource);

        var showDialogTask = _modifyFeaturesDialog.ShowAsync();

        await _window.DispatcherQueue.EnqueueAsync(async () =>
        {
            var exitCode = await ModifyWindowsOptionalFeatures.ModifyFeaturesAsync(Features, _log, cancellationTokenSource.Token);

            await LoadFeaturesCommand.ExecuteAsync(null);
            _restartNeeded = HasFeatureStatusChanged();
            if (_restartNeeded)
            {
                ShowRestartNotification();
            }

            switch (exitCode)
            {
                case ModifyWindowsOptionalFeatures.ExitCode.Success:
                    // Mark that changes have been applied and a restart is needed. This allows for a persistent notification
                    // to be displayed when the user navigates away from the page and returns.
                    if (_restartNeeded)
                    {
                        _modifyFeaturesDialog.ViewModel.SetCompleteRestartRequired();
                    }
                    else
                    {
                        _modifyFeaturesDialog.Hide();
                    }

                    break;
                case ModifyWindowsOptionalFeatures.ExitCode.Failure:
                    _modifyFeaturesDialog.Hide();
                    ShowFailedToApplyAllNotification();
                    break;
                case ModifyWindowsOptionalFeatures.ExitCode.Cancelled:
                    // Do nothing for these conditions, the InfoBar will be updated by ModifyFeaturesAsync
                    // in these cases.
                    _modifyFeaturesDialog.Hide();
                    break;
            }
        });

        await showDialogTask;
    }

    private async Task OnFeaturesChanged()
    {
        await _window.DispatcherQueue.EnqueueAsync(() =>
        {
            OnPropertyChanged(nameof(FeaturesLoaded));
            OnPropertyChanged(nameof(HasFeatureChanges));
            OnPropertyChanged(nameof(ChangesCanBeApplied));
        });
    }

    public void ShowRestartNotification()
    {
        _notificationQueue?.ShowWithWindowExtension(
            _stringResource.GetLocalized("ChangesAppliedTitle"),
            _stringResource.GetLocalized("RestartRequiredMessage"),
            InfoBarSeverity.Warning,
            new RelayCommand(RestartHelper.RestartComputer),
            _stringResource.GetLocalized("RestartNowButtonText"));
    }

    public void ShowNonAdminUserNotification()
    {
        _notificationQueue?.ShowWithWindowExtension(
            _stringResource.GetLocalized("NonAdminUserTitle"),
            _stringResource.GetLocalized("NonAdminUserMessage"),
            InfoBarSeverity.Warning);
    }

    public void ShowFailedToApplyAllNotification()
    {
        _notificationQueue?.ShowWithWindowExtension(
            _stringResource.GetLocalized("FailedToApplyChangesTitle"),
            _stringResource.GetLocalized("FailedToApplyChangesMessage"),
            InfoBarSeverity.Error);
    }
}
