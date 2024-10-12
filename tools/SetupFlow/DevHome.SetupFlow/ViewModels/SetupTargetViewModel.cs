// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.ViewModels;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupTargetViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SetupTargetViewModel));

    private readonly DispatcherQueue _dispatcherQueue;

    private const string SortByDisplayName = "DisplayName";

    private readonly string _allKeyWordLocalized;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly ObservableCollection<ComputeSystemsListViewModel> _computeSystemViewModelList = new();

    private readonly ComputeSystemViewModelFactory _computeSystemViewModelFactory;

    private EnvironmentsNotificationHelper _notificationsHelper;

    private bool _shouldNavigateToExtensionPage;

    [ObservableProperty]
    private string _callToActionText;

    [ObservableProperty]
    private string _callToActionHyperLinkButtonText;

    [ObservableProperty]
    private bool _shouldShowCollectionView;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowList;

    [ObservableProperty]
    private string _computeSystemFilterText;

    [ObservableProperty]
    private ObservableCollection<string> _computeSystemProviderComboBoxNames;

    [NotifyPropertyChangedFor(nameof(ProviderComboBoxNamesCollectionView))]
    [NotifyCanExecuteChangedFor(nameof(SyncComputeSystemsCommand))]
    [ObservableProperty]
    private bool _computeSystemLoadingCompleted;

    [ObservableProperty]
    private AdvancedCollectionView _providerComboBoxNamesCollectionView;

    public ObservableCollection<string> ComputeSystemsSortOptions { get; private set; }

    public AdvancedCollectionView ComputeSystemsCollectionView { get; private set; }

    public IComputeSystemManager ComputeSystemManagerObj { get; private set; }

    public string SelectedComputeSystemProviderComboBoxName { get; set; }

    public int SelectedComputeSystemSortComboBoxIndex { get; set; }

    public ExtensionInstallationViewModel InstallationViewModel { get; }

    public SetupTargetViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowViewModel setupFlowModel,
        SetupFlowOrchestrator orchestrator,
        IComputeSystemManager computeSystemManager,
        ExtensionInstallationViewModel installationViewModel,
        ComputeSystemViewModelFactory computeSystemViewModelFactory,
        DispatcherQueue dispatcherQueue)
        : base(stringResource, orchestrator)
    {
        // Setup initial state for page.
        Orchestrator.CurrentSetupFlowKind = SetupFlowKind.SetupTarget;
        PageTitle = StringResource.GetLocalized(StringResourceKey.SetupTargetPageTitle);
        _allKeyWordLocalized = StringResource.GetLocalized(StringResourceKey.SetupTargetAllComboBoxOption);

        // Add the "All" option to the combo box and make sure its always sorted.
        SelectedComputeSystemProviderComboBoxName = _allKeyWordLocalized;
        _computeSystemProviderComboBoxNames = new() { SelectedComputeSystemProviderComboBoxName, };
        ProviderComboBoxNamesCollectionView = new AdvancedCollectionView(_computeSystemProviderComboBoxNames, true);
        ProviderComboBoxNamesCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));

        // Add sort options like A-Z, Z-A, etc.
        ComputeSystemsSortOptions = new ObservableCollection<string>
        {
            StringResource.GetLocalized(StringResourceKey.SetupTargetSortAToZLabel),
            StringResource.GetLocalized(StringResourceKey.SetupTargetSortZToALabel),
        };

        // Add AdvancedCollectionView to make filtering and sorting the list of ComputeSystemsListViewModels easier.
        ComputeSystemsCollectionView = new AdvancedCollectionView(_computeSystemViewModelList, true);

        _dispatcherQueue = dispatcherQueue;
        _computeSystemViewModelFactory = computeSystemViewModelFactory;
        ComputeSystemManagerObj = computeSystemManager;
        _setupFlowViewModel = setupFlowModel;
        _setupFlowViewModel.EndSetupFlow += OnRemovingComputeSystems;
        InstallationViewModel = installationViewModel;
        InstallationViewModel.ExtensionChangedEvent += OnExtensionsChanged;
    }

    public void OnExtensionsChanged(object sender, EventArgs args)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await GetComputeSystemsAsync();
        });
    }

    /// <summary>
    /// When the ComputeSystemManager is removing compute systems, we need to remove the event handlers from the ListViewModels.
    /// And we need to remove the ListViewModels from the list of ListViewModels.
    /// </summary>
    /// <param name="sender">object that ends the setup flow</param>
    /// <param name="e">An empty event arg</param>
    private void OnRemovingComputeSystems(object sender, EventArgs e)
    {
        // remove event handlers from ListViewModels and clear our list.
        RemoveComputeSystemsListViewModels();

        // reset the SetupFlowVersion to LocalMachine so we can start the flow over again.
        Orchestrator.CurrentSetupFlowKind = SetupFlowKind.LocalMachine;

        // Unsubscribe from the EndSetupFlow event handler.
        _setupFlowViewModel.EndSetupFlow -= OnRemovingComputeSystems;
        InstallationViewModel.ExtensionChangedEvent -= OnExtensionsChanged;
    }

    /// <summary>
    /// When the user types in the filter text box, we want to filter the list of ComputeSystemsListViewModels and the list of ComputeSystemCardViewModels
    /// that they contain.
    /// </summary>
    /// <param name="text">Text that the user enters into the filter textbox.</param>
    [RelayCommand]
    public void FilterTextChanged(string text)
    {
        ComputeSystemsCollectionView.Filter = item =>
        {
            if (item is ComputeSystemsListViewModel listViewModel)
            {
                return ShouldShowListInUI(listViewModel, text);
            }

            return false;
        };
    }

    /// <summary>
    /// When the user selects a provider from the combo box, we want to filter the list of ComputeSystemsListViewModels and the list of ComputeSystemCardViewModels
    /// that they contain.
    /// </summary>
    /// <param name="text">The ComputeSystemProvider display name.</param>
    [RelayCommand]
    public void FilterComboBoxChanged(string text)
    {
        FilterTextChanged(ComputeSystemFilterText);
    }

    /// <summary>
    /// Filters the list of ComputeSystemsListViewModels and the list of ComputeSystemCardViewModels that they contain. Based
    /// on the text entered in the filter textbox and the provider selected in the combo box.
    /// </summary>
    private bool ShouldShowListInUI(ComputeSystemsListViewModel listViewModel, string text)
    {
        try
        {
            var shouldShowAllProviders = CompareStrings(_allKeyWordLocalized, SelectedComputeSystemProviderComboBoxName);
            var providerNameMatchesCurrentProvider = CompareStrings(listViewModel.DisplayName, SelectedComputeSystemProviderComboBoxName);

            if (!shouldShowAllProviders && !providerNameMatchesCurrentProvider)
            {
                RemoveSelectedItemIfNotInUI(listViewModel);
                return false;
            }

            // we need to filter the ComputeSystemCardViewModels so only those that contain the text show up in the UI.
            listViewModel.FilterComputeSystemCards(text);
            if (listViewModel.ComputeSystemCardAdvancedCollectionView.Count == 0)
            {
                RemoveSelectedItemIfNotInUI(listViewModel);
                return false;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error filtering ComputeSystemsListViewModel");
        }

        return true;
    }

    /// <summary>
    /// Compares two strings and returns true if they are equal. This method is case insensitive.
    /// </summary>
    /// <param name="text">First string to use in comparison</param>
    /// <param name="text2">Second string to use in comparison</param>
    private bool CompareStrings(string text, string text2)
    {
        return string.Equals(text, text2, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// _computeSystemViewModelList contains a list of ComputeSystemsListViewModel objects who also contain a list of ComputeSystemCardViewModels.
    /// Each ComputeSystemsListViewModel object has no way of knowing about selections in other ComputeSystemsListViewModel objects. Because of this,
    /// since we want to select only one ComputeSystemCardViewModel from one ComputeSystemsListViewModel at a time, we need to deselect
    /// all other cards in every other ComputeSystemsListViewModel object except for the one the user selected. This method will de-select
    /// all the cards in the other ListViewModels and set the ComputeSystemManager's ComputeSystemSetupItem property to the
    /// ComputeSystem and provider associated with the currently selected ComputeSystemCardViewModel.
    /// </summary>
    /// <param name="sender">The ComputeSystemsListViewModel object that contains the ComputeSystemCardViewModel the user selected.</param>
    /// <param name="computeSystem">The compute system wrapper associated with the ComputeSystemCardViewModel.</param>
    public void OnListSelectionChanged(object sender, ComputeSystemCache computeSystem)
    {
        if (sender is not ComputeSystemsListViewModel senderListViewModel)
        {
            return;
        }

        foreach (var viewModel in _computeSystemViewModelList)
        {
            if (senderListViewModel != viewModel)
            {
                viewModel.SelectedItem = null;
                viewModel.SetAllSelectionFlagsToFalse();
            }
        }

        ComputeSystemManagerObj.ComputeSystemSetupItem = new(computeSystem, senderListViewModel.Provider);
        UpdateNextButtonState();
    }

    public bool CanEnableSyncButton()
    {
        return ComputeSystemLoadingCompleted;
    }

    [RelayCommand(CanExecute = nameof(CanEnableSyncButton))]
    public async Task SyncComputeSystems()
    {
        await GetComputeSystemsAsync();
    }

    /// <summary>
    /// Make sure we only get the list of ComputeSystems from the ComputeSystemManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        await GetComputeSystemsAsync();
    }

    public void UpdateNextButtonState()
    {
        // Only enable the next button if the ComputeSystemManager has a ComputeSystem selected to apply the configuration to.
        // and if the ComputeSystemManager has finished loading the compute systems.
        CanGoToNextPage = ComputeSystemManagerObj.ComputeSystemSetupItem != null;
        Orchestrator.NotifyNavigationCanExecuteChanged();
        SyncComputeSystemsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts the process of getting the list of ComputeSystems from all providers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private async Task GetComputeSystemsAsync()
    {
        // Remove any existing ComputeSystemsListViewModels from the list if they exist. E.g when sync button is
        // pressed.
        RemoveComputeSystemsListViewModels();
        CallToActionText = null;
        CallToActionHyperLinkButtonText = null;
        _shouldNavigateToExtensionPage = false;

        // Disable the sync and next buttons while we're getting the compute systems.
        ComputeSystemLoadingCompleted = false;
        UpdateNextButtonState();
        _notificationsHelper?.ClearNotifications();

        // load the compute systems so we can show them in the UI.
        await Task.Run(LoadAllComputeSystemsInTheUIAsync);
        ShouldShowShimmerBelowList = false;
        ComputeSystemLoadingCompleted = true;

        // Enable the sync and next buttons when we're done getting the compute systems.
        UpdateNextButtonState();

        ComputeSystemsCollectionView.Refresh();

        // No compute systems found, show the call to action UI
        if (_computeSystemViewModelList.Count == 0)
        {
            var providerCountWithOutAllKeyword = ComputeSystemProviderComboBoxNames.Count - 1;

            var callToActionData = ComputeSystemHelpers.UpdateCallToActionText(providerCountWithOutAllKeyword);
            _shouldNavigateToExtensionPage = callToActionData.NavigateToExtensionsLibrary;
            CallToActionText = callToActionData.CallToActionText;
            CallToActionHyperLinkButtonText = callToActionData.CallToActionHyperLinkText;
        }
    }

    /// <summary>
    /// Removes all ComputeSystemsListViewModels from the list view model list and removes the compute system
    /// selected to apply the configuration to. This should refresh the UI to show no compute systems.
    /// </summary>
    private void RemoveComputeSystemsListViewModels()
    {
        var totalLists = _computeSystemViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _computeSystemViewModelList[i].CardSelectionChanged -= OnListSelectionChanged;
            _computeSystemViewModelList[i].SelectedItem = null;
            _computeSystemViewModelList[i].RemoveCardViewModelEventHandlers();
            _computeSystemViewModelList.RemoveAt(i);
        }

        var totalProviderNames = ComputeSystemProviderComboBoxNames.Count;
        for (var i = totalProviderNames - 1; i >= 0; i--)
        {
            if (!ComputeSystemProviderComboBoxNames[i].Equals(_allKeyWordLocalized, StringComparison.OrdinalIgnoreCase))
            {
                ComputeSystemProviderComboBoxNames.RemoveAt(i);
            }
        }

        // Reset the filter text and the selected provider name.
        ComputeSystemFilterText = string.Empty;
        SelectedComputeSystemProviderComboBoxName = _allKeyWordLocalized;

        ComputeSystemManagerObj.ComputeSystemSetupItem = null;
        ShouldShowCollectionView = false;
        ComputeSystemsCollectionView.Refresh();
        ProviderComboBoxNamesCollectionView.Refresh();
    }

    private void UpdateProviderNames(ComputeSystemsListViewModel listViewModel)
    {
        // Add provider name to combo box list.
        if (!ComputeSystemProviderComboBoxNames.Contains(listViewModel.DisplayName))
        {
            ComputeSystemProviderComboBoxNames.Add(listViewModel.DisplayName);
        }
    }

    /// <summary>
    /// Adds a ComputeSystemsListViewModel from the ComputeSystemManager.
    /// </summary>
    private void AddListViewModelToList(ComputeSystemsListViewModel listViewModel)
    {
        UpdateProviderNames(listViewModel);

        // Subscribe to the listViewModel's SelectionChanged event.
        listViewModel.CardSelectionChanged += OnListSelectionChanged;

        // listViewModel doesn't exist so add it to the list.
        _computeSystemViewModelList.Add(listViewModel);
        ShouldShowCollectionView = true;
    }

    /// <summary>
    /// Loads all the ComputeSystems from all providers and updates the UI with the results.
    /// </summary>
    public async Task LoadAllComputeSystemsInTheUIAsync()
    {
        try
        {
            await ComputeSystemManagerObj.GetComputeSystemsAsync(UpdateListViewModelList);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error loading ComputeSystemViewModels data");
        }
    }

    private void RemoveSelectedItemIfNotInUI(ComputeSystemsListViewModel listViewModel)
    {
        if (listViewModel.SelectedItem != null)
        {
            // If the user had a ComputeSystemCardViewModel selected and then they filtered
            // the list so that the ComputeSystemCardViewModel doesn't show up in the UI anymore,
            // we need to clear the ComputeSystemManager's ComputeSystemSetupItem property.
            ComputeSystemManagerObj.ComputeSystemSetupItem = null;
            listViewModel.SelectedItem = null;
            UpdateNextButtonState();
        }
    }

    public async Task UpdateListViewModelList(ComputeSystemsLoadedData data)
    {
        _notificationsHelper?.DisplayComputeSystemEnumerationErrors(data);

        var computeSystemListViewModels = new List<ComputeSystemsListViewModel>();
        var allComputeSystems = new List<ComputeSystemCache>();
        foreach (var devIdToComputeSystemResultPair in data.DevIdToComputeSystemMap)
        {
            // Remove the mappings that failed to load.
            // The errors are already handled by the notification helper.
            if (devIdToComputeSystemResultPair.Value.Result.Status == ProviderOperationStatus.Failure)
            {
                continue;
            }

            var listViewModel = new ComputeSystemsListViewModel(data.ProviderDetails, devIdToComputeSystemResultPair);

            if (listViewModel.ComputeSystems.Count > 0)
            {
                computeSystemListViewModels.Add(listViewModel);
                allComputeSystems.AddRange(listViewModel.ComputeSystems);
            }
        }

        // Fetch data for all compute systems that support the ApplyConfiguration flag in parallel
        // on thread pool to avoid calling expensive OOP operations on the UI thread.
        await Parallel.ForEachAsync(allComputeSystems, async (computeSystem, token) =>
        {
            await computeSystem.FetchDataAsync();
        });

        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            foreach (var listViewModel in computeSystemListViewModels)
            {
                foreach (var computeSystem in listViewModel.ComputeSystems)
                {
                    var packageFullName = data.ProviderDetails.ExtensionWrapper.PackageFullName;
                    var card = await _computeSystemViewModelFactory.CreateCardViewModelAsync(
                        ComputeSystemManagerObj,
                        computeSystem,
                        data.ProviderDetails.ComputeSystemProvider,
                        packageFullName,
                        _dispatcherQueue);

                    // Don't show environments that aren't in a state to configure
                    if (!ShouldShowCard(card.CardState))
                    {
                        _log.Information($"{computeSystem.DisplayName} not in valid state." +
                            $" Current state: {card.CardState}");
                        continue;
                    }

                    listViewModel.ComputeSystemCardCollection.Add(card);
                }

                if (listViewModel.ComputeSystemCardCollection.Count > 0)
                {
                    AddListViewModelToList(listViewModel);
                    listViewModel.CardSelectionChanged += OnListSelectionChanged;
                }
            }

            ShouldShowShimmerBelowList = true;
        });
    }

    /// <summary>
    /// Sorts the list of ComputeSystemsListViewModels based on the selected sort option.
    /// </summary>
    /// <param name="index">The current index of the combo box the user selected</param>
    [RelayCommand]
    public void SortComboBoxChanged(int index)
    {
        try
        {
            var direction = index == 0 ? SortDirection.Ascending : SortDirection.Descending;
            ComputeSystemsCollectionView.SortDescriptions.Clear();
            ComputeSystemsCollectionView.SortDescriptions.Add(new SortDescription(SortByDisplayName, direction));

            foreach (var viewModel in _computeSystemViewModelList)
            {
                // For now , we only support sorting by the ComputeSystemTitle.
                viewModel.SortBySpecificProperty(SortByKind.ComputeSystemTitle, direction);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Unable to perform sort operation");
        }
    }

    [RelayCommand]
    private async Task OnLoadedAsync(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(notificationQueue);
        await InstallationViewModel.UpdateExtensionPackageInfoAsync();
    }

    /// <summary>
    /// Navigates the user to the create environment flow.
    /// </summary>
    [RelayCommand]
    public void CallToActionButton()
    {
        Orchestrator.NavigateToOutsideFlow(KnownPageKeys.SetupFlow, "startCreationFlow;SetupEnvironmentPage");
    }

    private bool ShouldShowCard(ComputeSystemState state)
    {
        switch (state)
        {
            case ComputeSystemState.Creating:
            case ComputeSystemState.Deleting:
            case ComputeSystemState.Deleted:
                return false;
            default:
                return true;
        }
    }
}
