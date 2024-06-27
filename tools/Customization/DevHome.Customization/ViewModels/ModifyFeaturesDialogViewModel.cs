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

    private CancellationTokenSource? _cancellationTokenSource;

    public enum State
    {
        Initial,
        CommittingChanges,
        Complete,
    }

    [ObservableProperty]
    private State _currentState = State.Initial;

    public ModifyFeaturesDialogViewModel(IAsyncRelayCommand applyChangedCommand)
    {
        _stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        _applyChangesCommand = applyChangedCommand;
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _primaryButtonText = string.Empty;

    [ObservableProperty]
    private string _secondaryButtonText = string.Empty;

    [ObservableProperty]
    private bool _isPrimaryButtonEnabled;

    [ObservableProperty]
    private bool _isSecondaryButtonEnabled;

    [ObservableProperty]
    private bool _showProgress;

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

    public void SetCompleteRestartRequired()
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

    internal void HandlePrimaryButton()
    {
        switch (CurrentState)
        {
            case State.Complete:
                RestartHelper.RestartComputer();
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
        }
    }
}
