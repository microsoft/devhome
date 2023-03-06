// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.Loading.ViewModels;
using DevHome.SetupFlow.MainPage.ViewModels;
using DevHome.SetupFlow.Review.ViewModels;
using DevHome.SetupFlow.Summary.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupFlowViewModel : ObservableObject
{
    private readonly IHost _host;
    private readonly ILogger _logger;
    private readonly SetupFlowOrchestrator _orchestrator;
    private readonly MainPageViewModel _mainPageViewModel;
    private readonly List<SetupPageViewModelBase> _flowPages;
    private int _currentPageIndex;

    [ObservableProperty]
    private SetupPageViewModelBase _currentPageViewModel;

    public bool IsPreviousButtonVisible => _currentPageIndex > 0;

    private int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            _currentPageIndex = value;
            CurrentPageViewModel = _flowPages[_currentPageIndex];
            CurrentPageViewModel.OnNavigateToPageAsync();
            _orchestrator.NotifyNavigationCanExecuteChanged();
            OnPropertyChanged(nameof(IsPreviousButtonVisible));
        }
    }

    public SetupFlowViewModel(IHost host, ILogger logger, SetupFlowOrchestrator orchestrator)
    {
        _host = host;
        _logger = logger;
        _orchestrator = orchestrator;

        _orchestrator.SetNavigationButtonsCommands(new List<IRelayCommand> { GoToNextPageCommand, GoToPreviousPageCommand, CancelCommand });

        // Set initial view
        _mainPageViewModel = _host.GetService<MainPageViewModel>();
        _flowPages = new List<SetupPageViewModelBase>
        {
            _mainPageViewModel,
        };

        CurrentPageIndex = 0;

        _mainPageViewModel.StartSetupFlow += (object sender, IList<ISetupTaskGroup> taskGroups) =>
        {
            _orchestrator.TaskGroups = taskGroups;
            StartSetupFlowWithCurrentTaskGroups();
        };
    }

    private void StartSetupFlowWithCurrentTaskGroups()
    {
        _flowPages.Clear();
        _flowPages.AddRange(_orchestrator.TaskGroups.Select(flow => flow.GetSetupPageViewModel()));
        _flowPages.Add(_host.GetService<ReviewViewModel>());

        // The Loading page can advance to the next page
        // without user interaction once it is complete
        var loadingPageViewModel = _host.GetService<LoadingViewModel>();
        _flowPages.Add(loadingPageViewModel);

        /*
        loadingPageViewModel.ExecutionFinished += (object _, EventArgs _) =>
        {
            GoToNextPage();
        };
        */

        _flowPages.Add(_host.GetService<SummaryViewModel>());

        CurrentPageIndex = 0;
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    public void GoToPreviousPage()
    {
        CurrentPageIndex--;
    }

    private bool CanGoToPreviousPage()
    {
        return CurrentPageIndex > 0 && CurrentPageViewModel.CanGoToPreviousPage;
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    public void GoToNextPage()
    {
        CurrentPageIndex++;
    }

    private bool CanGoToNextPage()
    {
        return CurrentPageIndex + 1 < _flowPages.Count && CurrentPageViewModel.CanGoToNextPage;
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel()
    {
        _flowPages.Clear();
        _flowPages.Add(_mainPageViewModel);

        CurrentPageIndex = 0;
    }

    private bool CanCancel()
    {
        return CurrentPageViewModel.CanCancel;
    }
}
