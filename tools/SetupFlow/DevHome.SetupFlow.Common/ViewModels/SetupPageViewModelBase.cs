// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Common.Services;

namespace DevHome.SetupFlow.Common.ViewModels;

/// <summary>
/// Base view model class for all the pages in the Setup flow.
/// </summary>
public partial class SetupPageViewModelBase : ObservableObject
{
    /// <summary>
    /// Indicates whether this page has already executed <see cref="OnFirstNavigateToAsync"/>.
    /// </summary>
    private bool _hasExecutedFirstNavigateTo;

    /// <summary>
    /// Indicates whether this page has already executed <see cref="OnFirstNavigateFromAsync"/>
    /// </summary>
    private bool _hasExecutedFirstNavigateFrom;

    /// <summary>
    /// Indicates whether to show the navigation bar at the bottom of this page.
    /// </summary>
    [ObservableProperty]
    private bool _isNavigationBarVisible = true;

    /// <summary>
    /// Indicates whether the "Previous page" button should be enabled on this page.
    /// </summary>
    /// <remarks>
    /// This needs only be aware of reasons to disable the action that are local to the page.
    /// The containing orchestrator handles other conditions, like being the first page.
    /// </remarks>
    [ObservableProperty]
    private bool _canGoToPreviousPage = true;

    /// <summary>
    /// Indicates whether the "Next page" button should be enabled on this page.
    /// </summary>
    /// <remarks>
    /// This needs only be aware of reasons to disable the action that are local to the page.
    /// The containing orchestrator handles other conditions, like being the last page.
    /// </remarks>
    [ObservableProperty]
    private bool _canGoToNextPage = true;

    /// <summary>
    /// Text shown on the button that goes to the next page.
    /// By default, this will be "Next".
    /// </summary>
    [ObservableProperty]
    private string _nextPageButtonText;

    /// <summary>
    /// Indicates whether this page is one of the steps the user will need to complete before starting the setup.
    /// </summary>
    /// <remarks>
    /// This will allow us to add the "Step x of y" at the top of these pages.
    /// </remarks>
    [ObservableProperty]
    private bool _isStepPage = true;

    /// <summary>
    /// The title for the page. Used in the stepper control.
    /// </summary>
    [ObservableProperty]
    private string _pageTitle;

    /// <summary>
    /// Gets an object used to retrieve localized strings.
    /// </summary>
    protected ISetupFlowStringResource StringResource
    {
        get;
    }

    public SetupFlowOrchestrator Orchestrator
    {
        get;
    }

    public bool IsLastStepPage => IsStepPage && Orchestrator.SetupStepPages.LastOrDefault() == this;

    public bool IsPastPage => Orchestrator.IsPastPage(this);

    public bool IsCurrentPage => Orchestrator.IsCurrentPage(this);

    public bool IsUpcomingPage => Orchestrator.IsUpcomingPage(this);

    public SetupPageViewModelBase(ISetupFlowStringResource stringResource, SetupFlowOrchestrator orchestrator)
    {
        StringResource = stringResource;
        Orchestrator = orchestrator;
        _nextPageButtonText = StringResource.GetLocalized(StringResourceKey.Next);
    }

    /// <summary>
    /// Performs any work needed when navigating to a page.
    /// </summary>
    /// <remarks>
    /// The orchestrator takes care of calling this when appropriate.
    /// </remarks>
    public async Task OnNavigateToAsync()
    {
        if (!_hasExecutedFirstNavigateTo)
        {
            _hasExecutedFirstNavigateTo = true;
            await OnFirstNavigateToAsync();
        }
    }

    /// <summary>
    /// Performs any work needed when navigating away from a page.
    /// </summary>
    /// <remarks>
    /// The orchestrator takes care of calling this when appropriate.
    /// </remarks>
    public async Task OnNavigateFromAsync()
    {
        if (!_hasExecutedFirstNavigateFrom)
        {
            _hasExecutedFirstNavigateFrom = true;
            await OnFirstNavigateFromAsync();
        }
    }

    /// <summary>
    /// Hook for actions to execute the first time the page is loaded.
    /// </summary>
    /// <remarks>
    /// The orchestrator takes care of calling this when appropriate through <see cref="OnNavigateToAsync"/>.
    /// This runs on the UI thread, any time-consuming task should be non-blocking.
    /// Examples of uses include loading content to display on the page, or start
    /// background processing.
    /// </remarks>
    protected async virtual Task OnFirstNavigateToAsync()
    {
        // Do nothing
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hook for actions to execute immediately before navigating to the next page for the first time.
    /// </summary>
    /// <remarks>
    /// The orchestrator takes care of calling this when appropriate through <see cref="OnNavigateFromAsync"/>.
    /// This runs on the UI thread, any time-consuming task should be non-blocking.
    /// Example of uses include starting the elevated background process when leaving
    /// the review page.
    /// </remarks>
    protected async virtual Task OnFirstNavigateFromAsync()
    {
        // Do nothing
        await Task.CompletedTask;
    }
}
