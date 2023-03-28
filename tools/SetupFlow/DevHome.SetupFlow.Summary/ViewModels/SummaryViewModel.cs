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
        RepositoriesCloned.Add($@"github/dhoehna/Hello");
        RepositoriesCloned.Add($@"github/dhoehna/I");
        RepositoriesCloned.Add($@"github/dhoehna/Am");
        RepositoriesCloned.Add($@"github/dhoehna/Cool0");
        RepositoriesCloned.Add($@"github/dhoehna/Cool1");
        RepositoriesCloned.Add($@"github/dhoehna/Cool2");
        RepositoriesCloned.Add($@"github/dhoehna/Cool3");
        RepositoriesCloned.Add($@"github/dhoehna/Cool4");
        RepositoriesCloned.Add($@"github/dhoehna/Cool5");
        RepositoriesCloned.Add($@"github/dhoehna/Cool6");
        RepositoriesCloned.Add($@"github/dhoehna/Cool7");
        RepositoriesCloned.Add($@"github/dhoehna/Cool8");
        RepositoriesCloned.Add($@"github/dhoehna/Cool9");

        _appsDownloaded = new ();
        AppsDownloaded.Add("Postman00");
        AppsDownloaded.Add("Postman01");
        AppsDownloaded.Add("Postman02");
        AppsDownloaded.Add("Postman03");
        AppsDownloaded.Add("Postman04");
        AppsDownloaded.Add("Postman05");
        AppsDownloaded.Add("Postman06");
        AppsDownloaded.Add("Postman07");
        AppsDownloaded.Add("Postman08");
        AppsDownloaded.Add("Postman09");
        AppsDownloaded.Add("Postman10");
        AppsDownloaded.Add("Postman11");
        AppsDownloaded.Add("Postman12");
        AppsDownloaded.Add("Postman13");
        AppsDownloaded.Add("Postman14");
        AppsDownloaded.Add("Postman15");
        AppsDownloaded.Add("Postman16");
        AppsDownloaded.Add("Postman17");
        AppsDownloaded.Add("Postman18");
        AppsDownloaded.Add("Postman19");
        AppsDownloaded.Add("Postman20");
        AppsDownloaded.Add("Postman21");
        AppsDownloaded.Add("Postman22");
        AppsDownloaded.Add("Postman23");
        AppsDownloaded.Add("Postman24");
        AppsDownloaded.Add("Postman25");
        AppsDownloaded.Add("Postman26");
        AppsDownloaded.Add("Postman27");
        AppsDownloaded.Add("Postman28");
        AppsDownloaded.Add("Postman29");
        AppsDownloaded.Add("Postman30");
    }
}
