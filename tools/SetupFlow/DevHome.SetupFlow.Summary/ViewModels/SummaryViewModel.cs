// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.Summary.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly SetupFlowOrchestrator _orchestrator;

    [ObservableProperty]
    private Visibility _showRestartNeeded;

    [ObservableProperty]
    private ObservableCollection<string> _repositoriesCloned;

    [ObservableProperty]
    private ObservableCollection<string> _appsDownloaded;

    [RelayCommand]
    public void OpenDashboard()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void RemoveRestartGrid()
    {
        _showRestartNeeded = Visibility.Collapsed;
    }

    public SummaryViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        _orchestrator = orchestrator;

        IsNavigationBarVisible = false;
        IsStepPage = false;

        _showRestartNeeded = Visibility.Collapsed;
        _repositoriesCloned = new ();
        _appsDownloaded = new ();
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        _orchestrator.ReleaseRemoteFactory();
        await Task.CompletedTask;
    }
}
