// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;

namespace DevHome.SetupFlow.Common.ViewModels;

/// <summary>
/// Base view model class for all the pages in the Setup flow.
/// </summary>
public partial class SetupPageViewModelBase : ObservableObject
{
    //// TODO: Figure out how to notify change of state to the Can* properties to the RelayCommands controlling the buttons
    //// TODO: Figure out how the navigation in the setup flow should interact with navigation in the whole app (INavigationAware)

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
    /// Indicates whether the "Cancel" button should be enabled on this page.
    /// </summary>
    /// <remarks>
    /// This needs only be aware of reasons to disable the action that are local to the page.
    /// The containing orchestrator handles other conditions.
    /// </remarks>
    [ObservableProperty]
    private bool _canCancel = true;

    /// <summary>
    /// Indicates whether this page is one of the steps the user will need to complete before starting the setup.
    /// </summary>
    /// <remarks>
    /// This will allow us to add the "Step x of y" at the top of these pages.
    /// </remarks>
    //// TODO: Integrate this with Amir's SetupShell
    [ObservableProperty]
    private bool _isStepPage = true;

    /// <summary>
    /// Indicates whether this page has already executed <see cref="OnFirstNavigateToAsync"/>.
    /// </summary>
    private bool _hasExecutedFirstNavigateTo;

    /// <summary>
    /// Indicates whether this page has already executed <see cref="OnFirstNavigateFromAsync"/>
    /// </summary>
    private bool _hasExecutedFirstNavigateFrom;

    /// <summary>
    /// Gets an object used to retrieve localized strings.
    /// </summary>
    protected ISetupFlowStringResource StringResource
    {
        get; init;
    }

    public SetupPageViewModelBase(ISetupFlowStringResource stringResource)
    {
        StringResource = stringResource;
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
    /// This runs on the UI thread, any time-consuming task should ne non-blocking.
    /// Example of uses include starting the elevated background process when leaving
    /// the review page.
    /// </remarks>
    protected async virtual Task OnFirstNavigateFromAsync()
    {
        // Do nothing
        await Task.CompletedTask;
    }
}
