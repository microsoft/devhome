// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Base view model class for all the pages in the Setup flow.
/// </summary>
public partial class SetupPageViewModelBase : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SetupPageViewModelBase));

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
    /// Test shown as tool tip for the button that goes to the next page.
    /// By default this is empty.
    /// </summary>
    [ObservableProperty]
    private string _nextPageButtonToolTipText;

    /// <summary>
    /// Indicates whether this page is one of the steps the user will need to complete before starting the setup.
    /// </summary>
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
    /// This performs actions that need to be done only the first time we
    /// navigate to the page, and actions that need to be done each time.
    /// On the first run, the actions that only need to be done once are
    /// performed first.
    /// </remarks>
    public async Task OnNavigateToAsync()
    {
        if (!_hasExecutedFirstNavigateTo)
        {
            _hasExecutedFirstNavigateTo = true;
            _log.Information($"Executing post-navigation tasks for page {this.GetType().Name}");
            await OnFirstNavigateToAsync();
        }

        await OnEachNavigateToAsync();
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
            _log.Information($"Executing pre-navigation tasks for page {this.GetType().Name}");
            TelemetryFactory.Get<ITelemetry>().Log("PageNavigated", LogLevel.Critical, new PageNextSourceEvent(this.GetType().Name));
            await OnFirstNavigateFromAsync();
        }
    }

    /// <summary>
    /// Hook for actions to execute each time the page is shown.
    /// </summary>
    /// <remarks>
    /// This runs on the UI thread, any time-consuming task should be non-blocking.
    /// The first time the page is shown, this is executed after <see cref="OnFirstNavigateToAsync"/>
    /// </remarks>
    protected async virtual Task OnEachNavigateToAsync()
    {
        // Do nothing
        await Task.CompletedTask;
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
