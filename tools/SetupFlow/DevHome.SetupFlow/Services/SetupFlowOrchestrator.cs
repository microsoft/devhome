// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Elevation;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;

namespace DevHome.SetupFlow.Services;

public enum SetupFlowKind
{
    LocalMachine,
    SetupTarget,
    CreateEnvironment,
}

/// <summary>
/// Orchestrator for the Setup Flow, in charge of functionality across multiple pages.
/// </summary>
public partial class SetupFlowOrchestrator : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SetupFlowOrchestrator));

    private readonly List<SetupPageViewModelBase> _flowPages = new();

    private readonly INavigationService _navigationService;

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

    [ObservableProperty]
    private string _flowTitle;

    /// <summary>
    /// Gets a GUID that can be used to identify events related to the current setup flow in telemetry.
    /// This GUID is re-set each time we modify the pages in the flow.
    /// </summary>
    public Guid ActivityId
    {
        get; private set;
    }

    public bool IsSettingUpATargetMachine => CurrentSetupFlowKind == SetupFlowKind.SetupTarget;

    public bool IsSettingUpLocalMachine => CurrentSetupFlowKind == SetupFlowKind.LocalMachine;

    public bool IsInCreateEnvironmentFlow => CurrentSetupFlowKind == SetupFlowKind.CreateEnvironment;

    public AdaptiveCardFlowNavigator AdaptiveCardFlowNavigator { get; } = new();

    public SetupFlowOrchestrator(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    /// <summary>
    /// Occurs right before a page changes
    /// </summary>
    public event EventHandler PageChanging;

    /// <summary>
    /// Gets or sets the task groups that are to be executed on the flow.
    /// </summary>
    public IList<ISetupTaskGroup> TaskGroups
    {
        get; set;
    }

    public RemoteObject<IElevatedComponentOperation> RemoteElevatedOperation
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
            ActivityId = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Gets the pages that represents steps that the user has to take to start the setup.
    /// </summary>
    public IEnumerable<SetupPageViewModelBase> SetupStepPages => FlowPages.Where(page => page.IsStepPage);

    public bool HasPreviousPage => _currentPageIndex > 0;

    public bool IsMachineConfigurationInProgress => FlowPages.Count > 1;

    /// <summary>
    /// Gets or sets a value indicating whether the done button should be shown. When false, the cancel
    /// hyperlink button will be shown in the UI.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowDoneButton;

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
    public T GetTaskGroup<T>()
        where T : ISetupTaskGroup => TaskGroups.OfType<T>().FirstOrDefault();

    /// <summary>
    /// Releases the remote operation object, terminating the background process.
    /// </summary>
    public void ReleaseRemoteOperationObject()
    {
        // Disposing of this object signals the background process to terminate.
        RemoteElevatedOperation?.Dispose();
        RemoteElevatedOperation = null;
    }

    public SetupFlowKind CurrentSetupFlowKind { get; set; }

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

    public bool IsNavigatingForward { get; private set; }

    public bool IsNavigatingBackward { get; private set; }

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

    public async Task InitializeElevatedServerAsync()
    {
        _log.Information($"Initializing elevated server");
        var elevatedTasks = TaskGroups.SelectMany(taskGroup => taskGroup.SetupTasks.Where(task => task.RequiresAdmin));

        // If there are no elevated tasks, we don't need to create the remote object.
        if (elevatedTasks.Any())
        {
            TasksArguments tasksArguments = new()
            {
                InstallPackages = elevatedTasks.OfType<InstallPackageTask>().Select(task => task.GetArguments()).ToList(),
                Configure = elevatedTasks.OfType<ConfigureTask>().Select(task => task.GetArguments()).FirstOrDefault(),
                CreateDevDrive = elevatedTasks.OfType<CreateDevDriveTask>().Select(task => task.GetArguments()).FirstOrDefault(),
            };
            RemoteElevatedOperation = await IPCSetup.CreateOutOfProcessObjectAsync<IElevatedComponentOperation>(tasksArguments);
        }
        else
        {
            _log.Information($"Skipping elevated process initialization because no elevated tasks were found");
        }
    }

    private async Task SetCurrentPageIndex(int index)
    {
        IsNavigatingForward = index > _currentPageIndex;

        SetupPageViewModelBase previousPage = CurrentPageViewModel;

        // Update current page
        _currentPageIndex = index;
        CurrentPageViewModel = FlowPages.Any() ? FlowPages[_currentPageIndex] : null;
        _log.Information($"Moving to {CurrentPageViewModel?.GetType().Name}");

        // Last page in the setup flow should always be the summary page. The summary page is the only page where we show
        // the user the "Done" button.
        ShouldShowDoneButton = _currentPageIndex == FlowPages.Count - 1;

        // Do post-navigation tasks only when moving forwards, not when going back to a previous page.
        if (IsNavigatingForward)
        {
            await previousPage?.OnNavigateFromAsync();
        }

        IsNavigatingBackward = !IsNavigatingForward;
        await CurrentPageViewModel?.OnNavigateToAsync();

        // Reset navigation now that the navigation tasks are done.
        IsNavigatingForward = false;
        IsNavigatingBackward = false;
    }

    public void NavigateToOutsideFlow(string knownNavPageName, object parameter = null)
    {
        _log.Information($"Navigating to {knownNavPageName} with parameter: {parameter}");
        _navigationService.NavigateTo(knownNavPageName, parameter);
    }
}
