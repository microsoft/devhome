// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;

public partial class SetupTargetViewModel : SetupPageViewModelBase
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ToastNotificationService _toastNotificationService;

    private const string SortByDisplayName = "DisplayName";

    private readonly string _allKeyWordLocalized;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly ObservableCollection<ComputeSystemsListViewModel> _computeSystemViewModelList = new();

    private readonly ISetupFlowStringResource _setupFlowStringResource;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly ComputeSystemViewModelFactory _computeSystemViewModelFactory;

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

    public SetupTargetViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowViewModel setupflowModel,
        SetupFlowOrchestrator orchestrator,
        IComputeSystemManager computeSystemManager,
        ComputeSystemViewModelFactory computeSystemViewModelFactory,
        ToastNotificationService toastNotificationService)
        : base(stringResource, orchestrator)
    {
        // Setup initial state for page.
        _setupFlowOrchestrator = orchestrator;
        _setupFlowOrchestrator.CurrentSetupFlowKind = SetupFlowKind.SetupTarget;
        _setupFlowStringResource = stringResource;
        PageTitle = _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetPageTitle);
        _allKeyWordLocalized = _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetAllComboBoxOption);

        // Add the "All" option to the combo box and make sure its always sorted.
        SelectedComputeSystemProviderComboBoxName = _allKeyWordLocalized;
        _computeSystemProviderComboBoxNames = new() { SelectedComputeSystemProviderComboBoxName, };
        ProviderComboBoxNamesCollectionView = new AdvancedCollectionView(_computeSystemProviderComboBoxNames, true);
        ProviderComboBoxNamesCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));

        // Add sort options like A-Z, Z-A, etc.
        ComputeSystemsSortOptions = new ObservableCollection<string>
        {
            _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetSortAToZLabel),
            _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetSortZToALabel),
        };

        // Add AdvancedCollectionView to make filtering and sorting the list of ComputeSystemsListViewModels easier.
        ComputeSystemsCollectionView = new AdvancedCollectionView(_computeSystemViewModelList, true);

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _computeSystemViewModelFactory = computeSystemViewModelFactory;
        ComputeSystemManagerObj = computeSystemManager;
        _setupFlowViewModel = setupflowModel;
        _setupFlowViewModel.EndSetupFlow += OnRemovingComputeSystems;
        _toastNotificationService = toastNotificationService;
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
        _setupFlowOrchestrator.CurrentSetupFlowKind = SetupFlowKind.LocalMachine;

        // Unsubscribe from the EndSetupFlow event handler.
        _setupFlowViewModel.EndSetupFlow -= OnRemovingComputeSystems;
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

        ComputeSystemsCollectionView.Refresh();
    }

    /// <summary>
    /// When the user selects a provider from the combo box, we want to filter the list of ComputeSystemsListViewModels and the list of ComputeSystemCardViewModels
    /// that they contain.
    /// </summary>
    /// <param name="text">The ComputeSystemProvider display name.</param>
    [RelayCommand]
    public void FilterComboBoxChanged(string text)
    {
        FilterTextChangedCommand.Execute(ComputeSystemFilterText);
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

                // We still want to show the ComputeSystemsListViewModel in the UI if we're showing all providers or if the provider name matches the current provider.
                if (shouldShowAllProviders || providerNameMatchesCurrentProvider)
                {
                    return true;
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error filtering ComputeSystemsListViewModel", ex);
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
    /// ComputeSystem and provider assocated with the currently selected ComputeSystemCardViewModel.
    /// </summary>
    /// <param name="sender">The ComputeSystemsListViewModel object that contains the ComputeSystemCardViewModel the user selected.</param>
    /// <param name="computeSystem">The compute system wrapper associated with the ComputeSystemCardViewModel.</param>
    public void OnListSelectionChanged(object sender, ComputeSystem computeSystem)
    {
        if (sender is not ComputeSystemsListViewModel senderlistViewModel)
        {
            return;
        }

        foreach (var viewModel in _computeSystemViewModelList)
        {
            if (senderlistViewModel != viewModel)
            {
                viewModel.SelectedItem = null;
            }
        }

        ComputeSystemManagerObj.ComputeSystemSetupItem = new(computeSystem, senderlistViewModel.Provider);
        UpdateNextButtonState();
    }

    public bool CanEnableSyncButton()
    {
        return ComputeSystemLoadingCompleted;
    }

    [RelayCommand(CanExecute = nameof(CanEnableSyncButton))]
    public void SyncComputeSystems()
    {
        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _toastNotificationService.CheckIfUserIsAHyperVAdmin();
        GetComputeSystems();
    }

    /// <summary>
    /// Make sure we only get the list of ComputeSystems from the ComputeSystemManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        // Do nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;

        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _toastNotificationService.CheckIfUserIsAHyperVAdmin();

        GetComputeSystems();
    }

    public void UpdateNextButtonState()
    {
        // Only enable the next button if the ComputeSystemManager has a ComputeSystem selected to apply the configuration to.
        // and if the ComputeSystemManager has finished loading the compute systems.
        CanGoToNextPage = ComputeSystemManagerObj.ComputeSystemSetupItem != null;
        _setupFlowOrchestrator.NotifyNavigationCanExecuteChanged();
        SyncComputeSystemsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts the process of getting the list of ComputeSystems from all providers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetComputeSystems()
    {
        // We need to run this on a background thread so we don't block the UI thread.
        Task.Run(() =>
        {
            _dispatcher.EnqueueAsync(async () =>
            {
                // Remove any existing ComputeSystemsListViewModels from the list if they exist. E.g when sync button is
                // pressed.
                RemoveComputeSystemsListViewModels();

                // Disable the sync and next buttons while we're getting the compute systems.
                ComputeSystemLoadingCompleted = false;
                UpdateNextButtonState();

                // load the compute systems so we can show them in the UI.
                await LoadAllComputeSystemsInTheUI();

                // Enable the sync and next buttons when we're done getting the compute systems.
                UpdateNextButtonState();

                ComputeSystemsCollectionView.Refresh();
            });
        });
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
            ComputeSystemProviderComboBoxNames.Remove(_computeSystemViewModelList[i].DisplayName);
            _computeSystemViewModelList.RemoveAt(i);
        }

        // Reset the filter text and the selected provider name.
        ComputeSystemFilterText = string.Empty;
        SelectedComputeSystemProviderComboBoxName = _allKeyWordLocalized;

        ComputeSystemManagerObj.ComputeSystemSetupItem = null;
        ShouldShowCollectionView = false;
        ComputeSystemsCollectionView.Refresh();
        ProviderComboBoxNamesCollectionView.Refresh();
    }

    /// <summary>
    /// Adds a ComputeSystemsListViewModel from the ComputeSystemManager.
    /// </summary>
    private void AddListViewModelToList(ComputeSystemsListViewModel listViewModel)
    {
        // Add provider name to combo box list.
        if (!ComputeSystemProviderComboBoxNames.Contains(listViewModel.DisplayName))
        {
            ComputeSystemProviderComboBoxNames.Add(listViewModel.DisplayName);
        }

        // Subscribe to the listViewModel's SelectionChanged event.
        listViewModel.CardSelectionChanged += OnListSelectionChanged;

        // listViewModel doesn't exist so add it to the list.
        _computeSystemViewModelList.Add(listViewModel);
        ShouldShowCollectionView = true;
    }

    /// <summary>
    /// Loads all the ComputeSystems from all providers and updates the UI with the results.
    /// </summary>
    public async Task LoadAllComputeSystemsInTheUI()
    {
        try
        {
            await ComputeSystemManagerObj.GetComputeSystemsAsync(UpdateListViewModelList);
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading ComputeSystemViewModels data", ex);
        }

        ShouldShowShimmerBelowList = false;
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
        await _dispatcher.EnqueueAsync(async () =>
        {
            var curListViewModel = new ComputeSystemsListViewModel(data);

            foreach (var wrapper in curListViewModel.ComputeSystemWrappers)
            {
                // Remove any cards that don't support the ApplyConfiguration flag.
                if (!wrapper.SupportedOperations.HasFlag(ComputeSystemOperations.ApplyConfiguration))
                {
                    continue;
                }

                var packageFullName = data.ProviderDetails.ExtensionWrapper.PackageFullName;
                var card = await _computeSystemViewModelFactory.CreateCardViewModelAsync(ComputeSystemManagerObj, wrapper, curListViewModel.Provider, packageFullName);
                curListViewModel.ComputeSystemCardCollection.Add(card);
                curListViewModel.CardSelectionChanged += OnListSelectionChanged;
            }

            AddListViewModelToList(curListViewModel);
            ComputeSystemLoadingCompleted = true;
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
        var direction = index == 0 ? SortDirection.Ascending : SortDirection.Descending;
        ComputeSystemsCollectionView.SortDescriptions.Clear();
        ComputeSystemsCollectionView.SortDescriptions.Add(new SortDescription(SortByDisplayName, direction));

        foreach (var viewModel in _computeSystemViewModelList)
        {
            // For now , we only support sorting by the ComputeSystemTitle.
            viewModel.SortBySpecificProperty(SortByKind.ComputeSystemTitle, direction);
        }
    }
}
