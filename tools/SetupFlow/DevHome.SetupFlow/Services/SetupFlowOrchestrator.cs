// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.ElevatedComponent;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Orchestrator for the Setup Flow, in charge of functionality across multiple pages.
/// </summary>
[ObservableObject]
public partial class SetupFlowOrchestrator
{
    private readonly List<SetupPageViewModelBase> _flowPages = new ();

    /// <summary>
    /// Index for the current page in the <see cref="_flowPages"/>.
    /// </summary>
    /// <remarks>
    /// This must only be set through <see cref="SetCurrentPageIndex(int)"/> to ensure
    /// that all the changed properties are notified when updating <see cref="CurrentPageViewModel"/>.
    /// We don't have the NotifyPropertyChangedFor here because sometimes the page
    /// changes without the index changing (when setting the pages to a new list and
    /// the index to 0).
    /// </remarks>
    private int _currentPageIndex;

    /// <summary>
    /// The view model for the current page, or null if the pages have not been set.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetupStepPages))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
    private SetupPageViewModelBase _currentPageViewModel;

    /// <summary>
    /// Occurrs right before a page changes
    /// </summary>
    public event EventHandler PageChanging;

    /// <summary>
    /// Gets or sets the task groups that are to be executed on the flow.
    /// </summary>
    public IList<ISetupTaskGroup> TaskGroups
    {
        get; set;
    }

    public RemoteObject<IElevatedComponentFactory> RemoteElevatedFactory
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
            _ = SetCurrentPageIndex(0);
        }
    }

    /// <summary>
    /// Gets the pages that represents steps that the user has to take to start the setup.
    /// </summary>
    public IEnumerable<SetupPageViewModelBase> SetupStepPages => FlowPages.Where(page => page.IsStepPage);

    public bool HasPreviousPage => _currentPageIndex > 0;

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
    /// Gets the task group from the corresponding type, if it exists in the current flow.
    /// </summary>
#nullable enable
    public T? GetTaskGroup<T>()
        where T : ISetupTaskGroup
    {
        foreach (var taskGroup in TaskGroups)
        {
            if (taskGroup is T)
            {
                return (T)taskGroup;
            }
        }

        return default;
    }
#nullable disable

    /// <summary>
    /// Releases the remote factory, terminating the background process.
    /// </summary>
    public void ReleaseRemoteFactory()
    {
        // Disposing of this object signals the background process to terminate.
        RemoteElevatedFactory?.Dispose();
        RemoteElevatedFactory = null;
    }

    /// <summary>
    /// Determines whether a given page is one that was shown previously on the flow.
    /// </summary>
    public bool IsPastPage(SetupPageViewModelBase page) => FlowPages.Take(_currentPageIndex).Contains(page);

    /// <summary>
    /// Determines whether a given page is the one currently being shown.
    /// </summary>
    public bool IsCurrentPage(SetupPageViewModelBase page) => page == CurrentPageViewModel;

    /// <summary>
    /// Determines whether a given page is one that will be shown later in the flow.
    /// </summary>
    public bool IsUpcomingPage(SetupPageViewModelBase page) => FlowPages.Skip(_currentPageIndex + 1).Contains(page);

    partial void OnCurrentPageViewModelChanging(SetupPageViewModelBase value) => PageChanging?.Invoke(null, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    public async Task GoToPreviousPage()
    {
        await SetCurrentPageIndex(_currentPageIndex - 1);
    }

    private bool CanGoToPreviousPage()
    {
        return HasPreviousPage && CurrentPageViewModel.CanGoToPreviousPage;
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    public async Task GoToNextPage()
    {
        await SetCurrentPageIndex(_currentPageIndex + 1);
    }

    private bool CanGoToNextPage()
    {
        return _currentPageIndex + 1 < _flowPages.Count && CurrentPageViewModel.CanGoToNextPage;
    }

    private async Task SetCurrentPageIndex(int index)
    {
        var movingForward = index > _currentPageIndex;

        SetupPageViewModelBase previousPage = CurrentPageViewModel;

        // Update current page
        _currentPageIndex = index;
        CurrentPageViewModel = FlowPages.Any() ? FlowPages[_currentPageIndex] : null;
        Log.Logger?.ReportInfo(nameof(SetupFlowOrchestrator), $"Moving to {CurrentPageViewModel?.GetType().Name}");

        // Do post-navigation tasks only when moving forwards, not when going back to a previous page.
        if (movingForward)
        {
            await previousPage?.OnNavigateFromAsync();
        }

        await CurrentPageViewModel?.OnNavigateToAsync();
    }
}
