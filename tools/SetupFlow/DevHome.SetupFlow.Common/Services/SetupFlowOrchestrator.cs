// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;

namespace DevHome.SetupFlow.Common.Services;

/// <summary>
/// Orchestrator for the Setup Flow, in charge of functionality across multiple pages.
/// </summary>
[ObservableObject]
public partial class SetupFlowOrchestrator
{
    private readonly List<SetupPageViewModelBase> _flowPages = new ();

    /// <summary>
    /// Relay commands associated with the navigation buttons in the UI.
    /// </summary>
    private readonly List<IRelayCommand> _navigationButtonsCommands = new ();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPageViewModel))]
    [NotifyPropertyChangedFor(nameof(SetupStepPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private int _currentPageIndex;

    /// <summary>
    /// Gets the view model for the current page, or null if the pages have not been set.
    /// </summary>
    public SetupPageViewModelBase CurrentPageViewModel => FlowPages.Any() ? FlowPages[CurrentPageIndex] : null;

    /// <summary>
    /// Gets or sets the task groups that are to be executed on the flow.
    /// </summary>
    public IList<ISetupTaskGroup> TaskGroups
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the list of pages to be shown in this flow.
    /// </summary>
    /// <remarks>
    /// Setting the elements for this list is done on the SetupFlowViewModel
    /// as it requires referencing the specific page view models, and doing it
    /// here would cause a cyclic project reference.
    /// It is set there, but we keep the list here to be able to have the
    /// specific page view models know about their position in the overall flow.
    /// </remarks>
    public IReadOnlyList<SetupPageViewModelBase> FlowPages
    {
        get => _flowPages;
        set
        {
            _flowPages.Clear();
            _flowPages.AddRange(value);
            CurrentPageIndex = 0;
        }
    }

    /// <summary>
    /// Gets the pages that represents steps that the user has to take to start the setup.
    /// </summary>
    public IEnumerable<SetupPageViewModelBase> SetupStepPages => FlowPages.Where(page => page.IsStepPage);

    public bool HasPreviousPage => CurrentPageIndex > 0;

    /// <summary>
    /// Sets the list of commands associated with the navigation buttons.
    /// </summary>
    public void SetNavigationButtonsCommands(IList<IRelayCommand> navigationButtonsCommands)
    {
        _navigationButtonsCommands.Clear();
        _navigationButtonsCommands.AddRange(navigationButtonsCommands);
    }

    /// <summary>
    /// Notify all the navigation buttons that the CanExecute property has changed.
    /// </summary>
    /// <remarks>
    /// This is used so that the individual pages can notify the navigation container
    /// about changes in state without having to reach into the navigation container.
    /// We could notify each button specifically, but this is simpler and not too bad.
    /// </remarks>
    public void NotifyNavigationCanExecuteChanged()
    {
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Determines whether a given page is one that was shown previously on the flow.
    /// </summary>
    public bool IsPastPage(SetupPageViewModelBase page) => FlowPages.Take(CurrentPageIndex - 1).Contains(page);

    /// <summary>
    /// Determines whether a given page is the one currently being shown.
    /// </summary>
    public bool IsCurrentPage(SetupPageViewModelBase page) => page == CurrentPageViewModel;

    /// <summary>
    /// Determines whether a given page is one that will be shown later in the flow.
    /// </summary>
    public bool IsUpcomingPage(SetupPageViewModelBase page) => FlowPages.Skip(CurrentPageIndex + 1).Contains(page);

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    public async Task GoToPreviousPage()
    {
        await SetCurrentPageIndex(CurrentPageIndex - 1);
    }

    private bool CanGoToPreviousPage()
    {
        return HasPreviousPage && CurrentPageViewModel.CanGoToPreviousPage;
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    public async Task GoToNextPage()
    {
        await SetCurrentPageIndex(CurrentPageIndex + 1);
    }

    private bool CanGoToNextPage()
    {
        return CurrentPageIndex + 1 < _flowPages.Count && CurrentPageViewModel.CanGoToNextPage;
    }

    private async Task SetCurrentPageIndex(int index)
    {
        var movingForward = index > CurrentPageIndex;

        SetupPageViewModelBase previousPage = CurrentPageViewModel;

        // Update current page
        CurrentPageIndex = index;

        // Do post-navigation tasks only when moving forwards, not when going back to a previous page.
        if (movingForward)
        {
            await previousPage?.OnNavigateFromAsync();
        }

        await CurrentPageViewModel?.OnNavigateToAsync();
    }
}
