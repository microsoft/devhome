// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.Common.Services;

namespace DevHome.Customization.ViewModels;

public partial class ModifyFeaturesDialogViewModel : ObservableObject
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

    public ModifyFeaturesDialogViewModel(IAsyncRelayCommand applyChangedCommand, IAsyncRelayCommand discardChangesCommand)
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

    internal void HandlePrimaryButton()
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

    internal void HandleSecondaryButton()
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
