// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class VirtualMachineManagementViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(VirtualMachineManagementViewModel));

    private readonly StringResource _stringResource;

    private readonly bool _isUserAdministrator = WindowsIdentityHelper.IsUserAdministrator();

    private readonly DispatcherQueue _dispatcherQueue;

    private OptionalFeatureNotificationHelper? _notificationsHelper;

    private ContentDialog? _modifyFeaturesDialog;

    public IAsyncRelayCommand LoadFeaturesCommand { get; }

    public bool FeaturesLoaded => !LoadFeaturesCommand.IsRunning;

    public IAsyncRelayCommand ApplyChangesCommand { get; }

    public IAsyncRelayCommand DiscardChangesCommand { get; }

    public bool ChangesCanBeApplied => HasFeatureChanges && !ApplyChangesCommand.IsRunning;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ObservableCollection<OptionalFeatureState> Features { get; } = new();

    public bool HasFeatureChanges => _isUserAdministrator && FeaturesLoaded && Features.Any(f => f.HasChanged);

    public bool CanDismissNotifications => _isUserAdministrator;

    private bool _restartNeeded;

    public ModifyFeaturesDialogState DialogState { get; }

    public VirtualMachineManagementViewModel(DispatcherQueue dispatcherQueue)
    {
        _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        _dispatcherQueue = dispatcherQueue;

        Breadcrumbs =
        [
            new(_stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(_stringResource.GetLocalized("VirtualMachineManagement_Header"), typeof(VirtualMachineManagementViewModel).FullName!)
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

        DiscardChangesCommand = new AsyncRelayCommand(DiscardChangesAsync);
        DiscardChangesCommand.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(DiscardChangesCommand.IsRunning))
            {
                await OnFeaturesChanged();
            }
        };

        DialogState = new ModifyFeaturesDialogState(ApplyChangesCommand, DiscardChangesCommand);

        _ = LoadFeaturesCommand.ExecuteAsync(null);
    }

    internal void Initialize(StackedNotificationsBehavior notificationQueue, ContentDialog modifyFeaturesDialog)
    {
        _notificationsHelper = new(notificationQueue, _log);
        _modifyFeaturesDialog = modifyFeaturesDialog;

        if (!_isUserAdministrator)
        {
            _dispatcherQueue.EnqueueAsync(_notificationsHelper.ShowNonAdminUserNotification);
        }

        if (_restartNeeded)
        {
            _dispatcherQueue.EnqueueAsync(_notificationsHelper.ShowRestartNotification);
        }
    }

    internal void Uninitialize()
    {
        _notificationsHelper = null;
        _modifyFeaturesDialog = null;
    }

    private async void FeatureState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OptionalFeatureState.IsEnabled))
        {
            await OnFeaturesChanged();
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
        var cancellationTokenSource = new CancellationTokenSource();
        DialogState.SetCommittingChanges(cancellationTokenSource);
        var showDialogTask = ShowStateDialogAsync();

        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var exitCode = await ModifyWindowsOptionalFeatures.ModifyFeaturesAsync(Features, _notificationsHelper, _log, cancellationTokenSource.Token);

            // Handle the exit code as needed, for example:
            switch (exitCode)
            {
                case ModifyWindowsOptionalFeatures.ExitCode.Success:
                    // Mark that changes have been applied and a restart is needed. This allows for a persistent notification
                    // to be displayed when the user navigates away from the page and returns.
                    DialogState.SetComplete();
                    _restartNeeded = true;
                    break;
                case ModifyWindowsOptionalFeatures.ExitCode.NoChange:
                case ModifyWindowsOptionalFeatures.ExitCode.Failure:
                    // Do nothing for these error conditions, the InfoBar will be updated by ModifyFeaturesAsync
                    // in these cases.
                    _modifyFeaturesDialog?.Hide();
                    break;
            }

            await LoadFeaturesCommand.ExecuteAsync(null);
        });

        await showDialogTask;
    }

    private async Task DiscardChangesAsync()
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            foreach (var feature in Features)
            {
                feature.IsEnabled = feature.Feature.IsEnabled;
            }
        });
    }

    private async Task OnFeaturesChanged()
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            OnPropertyChanged(nameof(FeaturesLoaded));
            OnPropertyChanged(nameof(HasFeatureChanges));
            OnPropertyChanged(nameof(ChangesCanBeApplied));
        });
    }

    public async Task ShowStateDialogAsync()
    {
        await _modifyFeaturesDialog?.ShowAsync();
    }

    public partial class ModifyFeaturesDialogState : ObservableObject
    {
        private readonly StringResource _stringResource;

        private readonly IAsyncRelayCommand _applyChangesCommand;

        private readonly IAsyncRelayCommand _discardChangesCommand;

        private CancellationTokenSource? _cancellationTokenSource;

        public enum State
        {
            Initial,
            CommittingChanges,
            Complete,
            NotApplied,
        }

        [ObservableProperty]
        private State _currentState = State.Initial;

        public ModifyFeaturesDialogState(IAsyncRelayCommand applyChangedCommand, IAsyncRelayCommand discardChangesCommand)
        {
            _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
            _applyChangesCommand = applyChangedCommand;
            _discardChangesCommand = discardChangesCommand;
        }

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;

        [ObservableProperty]
        private string primaryButtonText = string.Empty;

        [ObservableProperty]
        private string secondaryButtonText = string.Empty;

        [ObservableProperty]
        private bool isPrimaryButtonEnabled;

        [ObservableProperty]
        private bool isSecondaryButtonEnabled;

        [ObservableProperty]
        private bool showProgress;

        public void SetCommittingChanges(CancellationTokenSource cancellationTokenSource)
        {
            CurrentState = State.CommittingChanges;

            _cancellationTokenSource = cancellationTokenSource;
            IsPrimaryButtonEnabled = false;
            IsSecondaryButtonEnabled = true;
            ShowProgress = true;
            Title = _stringResource.GetLocalized("CommittingChangesTitle");
            Message = _stringResource.GetLocalized("CommittingChangesMessage");
            PrimaryButtonText = _stringResource.GetLocalized("RestartNowButtonText");
            SecondaryButtonText = _stringResource.GetLocalized("CancelButtonText");
        }

        public void SetComplete()
        {
            CurrentState = State.Complete;

            _cancellationTokenSource = null;
            IsPrimaryButtonEnabled = true;
            IsSecondaryButtonEnabled = true;
            ShowProgress = false;
            Title = _stringResource.GetLocalized("RestartRequiredTitle");
            Message = _stringResource.GetLocalized("RestartRequiredMessage");
            PrimaryButtonText = _stringResource.GetLocalized("RestartNowButtonText");
            SecondaryButtonText = _stringResource.GetLocalized("DontRestartNowButtonText");
        }

        public void SetNotApplied()
        {
            CurrentState = State.NotApplied;

            _cancellationTokenSource = null;
            IsPrimaryButtonEnabled = true;
            IsSecondaryButtonEnabled = true;
            ShowProgress = false;
            Title = _stringResource.GetLocalized("ChangesNotAppliedTitle");
            Message = _stringResource.GetLocalized("ChangesNotAppliedMessage");
            PrimaryButtonText = _stringResource.GetLocalized("ApplyChangesButtonText");
            SecondaryButtonText = _stringResource.GetLocalized("DiscardChangesButtonText");
        }

        private void ResetState()
        {
            CurrentState = State.Initial;

            _cancellationTokenSource = null;
            IsPrimaryButtonEnabled = true;
            IsSecondaryButtonEnabled = true;
            ShowProgress = false;
            Title = string.Empty;
            Message = string.Empty;
            PrimaryButtonText = string.Empty;
            SecondaryButtonText = string.Empty;
        }

        [RelayCommand]
        private void HandlePrimaryButton()
        {
            switch (CurrentState)
            {
                case State.NotApplied:
                    _ = _applyChangesCommand.ExecuteAsync(null);
                    break;
                case State.Complete:
                    // Restart the machine
                    OptionalFeatureNotificationHelper.RestartComputer();
                    break;
                default:
                    ResetState();
                    break;
            }
        }

        [RelayCommand]
        private void HandleSecondaryButton()
        {
            switch (CurrentState)
            {
                case State.CommittingChanges:
                    _cancellationTokenSource?.Cancel();
                    break;
                case State.NotApplied:
                    _ = _discardChangesCommand.ExecuteAsync(null);
                    break;
                default:
                    ResetState();
                    break;
            }
        }
    }
}
