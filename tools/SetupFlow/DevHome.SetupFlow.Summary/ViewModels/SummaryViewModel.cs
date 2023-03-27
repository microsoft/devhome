// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.Summary.ViewModels;

public partial class SummaryViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;

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
        SetupFlowOrchestrator orchestrator,
        ILogger logger)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        IsNavigationBarVisible = false;
        IsStepPage = false;

        _showRestartNeeded = Visibility.Visible;
        _repositoriesCloned = new ();
        RepositoriesCloned.Add("Hello");
        RepositoriesCloned.Add("I");
        RepositoriesCloned.Add("Am");
        RepositoriesCloned.Add("Cool");

        _appsDownloaded = new ();
        AppsDownloaded.Add("Postman0");
        AppsDownloaded.Add("Postman1");
        AppsDownloaded.Add("Postman2");
        AppsDownloaded.Add("Postman3");
        AppsDownloaded.Add("Postman4");
        AppsDownloaded.Add("Postman5");
        AppsDownloaded.Add("Postman6");
    }
}
