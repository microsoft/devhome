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
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveInsightsViewModel : SetupPageViewModelBase
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ToastNotificationService _toastNotificationService;

    private const string SortByDisplayName = "DisplayName";

    private readonly string _allKeyWordLocalized;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly ObservableCollection<DevDrivesListViewModel> _devDriveViewModelList = new();

    private readonly ISetupFlowStringResource _setupFlowStringResource;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly DevDriveViewModelFactory _devDriveViewModelFactory;

    [ObservableProperty]
    private bool _shouldShowCollectionView;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowList;

    [ObservableProperty]
    private string _devDriveFilterText;

    [ObservableProperty]
    private ObservableCollection<string> _devDriveProviderComboBoxNames;

    [NotifyPropertyChangedFor(nameof(ProviderComboBoxNamesCollectionView))]
    [NotifyCanExecuteChangedFor(nameof(SyncDevDrivesCommand))]
    [ObservableProperty]
    private bool _devDriveLoadingCompleted;

    [ObservableProperty]
    private AdvancedCollectionView _providerComboBoxNamesCollectionView;

    public ObservableCollection<string> DevDrivesSortOptions { get; private set; }

    public AdvancedCollectionView DevDrivesCollectionView { get; private set; }

    public IDevDriveManager DevDriveManagerObj { get; private set; }

    public string SelectedDevDriveProviderComboBoxName { get; set; }

    public int SelectedDevDriveSortComboBoxIndex { get; set; }

    public DevDriveInsightsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowViewModel setupflowModel,
        SetupFlowOrchestrator orchestrator,
        IDevDriveManager devDriveManager,
        DevDriveViewModelFactory devDriveViewModelFactory,
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
        SelectedDevDriveProviderComboBoxName = _allKeyWordLocalized;
        _devDriveProviderComboBoxNames = new() { SelectedDevDriveProviderComboBoxName, };
        ProviderComboBoxNamesCollectionView = new AdvancedCollectionView(_devDriveProviderComboBoxNames, true);
        ProviderComboBoxNamesCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending));

        // Add sort options like A-Z, Z-A, etc.
        DevDrivesSortOptions = new ObservableCollection<string>
        {
            _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetSortAToZLabel),
            _setupFlowStringResource.GetLocalized(StringResourceKey.SetupTargetSortZToALabel),
        };

        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesListViewModels easier.
        DevDrivesCollectionView = new AdvancedCollectionView(_devDriveViewModelList, true);

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _devDriveViewModelFactory = devDriveViewModelFactory;
        DevDriveManagerObj = devDriveManager;
        _setupFlowViewModel = setupflowModel;
        _setupFlowViewModel.EndSetupFlow += OnRemovingDevDrives;
        _toastNotificationService = toastNotificationService;
    }

    /// <summary>
    /// When the DevDriveManager is removing dev drives, we need to remove the event handlers from the ListViewModels.
    /// And we need to remove the ListViewModels from the list of ListViewModels.
    /// </summary>
    /// <param name="sender">object that ends the setup flow</param>
    /// <param name="e">An empty event arg</param>
    private void OnRemovingDevDrives(object sender, EventArgs e)
    {
        // remove event handlers from ListViewModels and clear our list.
        RemoveDevDrivesListViewModels();

        // reset the SetupFlowVersion to LocalMachine so we can start the flow over again.
        _setupFlowOrchestrator.CurrentSetupFlowKind = SetupFlowKind.LocalMachine;

        // Unsubscribe from the EndSetupFlow event handler.
        _setupFlowViewModel.EndSetupFlow -= OnRemovingDevDrives;
    }

    /// <summary>
    /// When the user types in the filter text box, we want to filter the list of DevDrivesListViewModels and the list of DevDriveCardViewModels
    /// that they contain.
    /// </summary>
    /// <param name="text">Text that the user enters into the filter textbox.</param>
    [RelayCommand]
    public void FilterTextChanged(string text)
    {
        DevDrivesCollectionView.Filter = item =>
        {
            if (item is DevDrivesListViewModel listViewModel)
            {
                return ShouldShowListInUI(listViewModel, text);
            }

            return false;
        };

        DevDrivesCollectionView.Refresh();
    }

    /// <summary>
    /// When the user selects a provider from the combo box, we want to filter the list of DevDrivesListViewModels and the list of DevDriveCardViewModels
    /// that they contain.
    /// </summary>
    /// <param name="text">The DevDriveProvider display name.</param>
    [RelayCommand]
    public void FilterComboBoxChanged(string text)
    {
        FilterTextChangedCommand.Execute(DevDriveFilterText);
    }

    /// <summary>
    /// Filters the list of DevDrivesListViewModels and the list of DevDriveCardViewModels that they contain. Based
    /// on the text entered in the filter textbox and the provider selected in the combo box.
    /// </summary>
    private bool ShouldShowListInUI(DevDrivesListViewModel listViewModel, string text)
    {
        try
        {
            var shouldShowAllProviders = CompareStrings(_allKeyWordLocalized, SelectedDevDriveProviderComboBoxName);
            var providerNameMatchesCurrentProvider = CompareStrings(listViewModel.DisplayName, SelectedDevDriveProviderComboBoxName);

            if (!shouldShowAllProviders && !providerNameMatchesCurrentProvider)
            {
                RemoveSelectedItemIfNotInUI(listViewModel);
                return false;
            }

            // we need to filter the DevDriveCardViewModels so only those that contain the text show up in the UI.
            listViewModel.FilterDevDriveCards(text);
            if (listViewModel.DevDriveCardAdvancedCollectionView.Count == 0)
            {
                RemoveSelectedItemIfNotInUI(listViewModel);

                // We still want to show the DevDrivesListViewModel in the UI if we're showing all providers or if the provider name matches the current provider.
                if (shouldShowAllProviders || providerNameMatchesCurrentProvider)
                {
                    return true;
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error filtering DevDrivesListViewModel", ex);
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

    public bool CanEnableSyncButton()
    {
        return DevDriveLoadingCompleted;
    }

    [RelayCommand(CanExecute = nameof(CanEnableSyncButton))]
    public void SyncDevDrives()
    {
        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _toastNotificationService.CheckIfUserIsAHyperVAdmin();
        GetDevDrives();
    }

    /// <summary>
    /// Make sure we only get the list of DevDrives from the DevDriveManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    protected async override Task OnFirstNavigateToAsync()
    {
        // Do nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;

        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _toastNotificationService.CheckIfUserIsAHyperVAdmin();

        GetDevDrives();
    }

    public void UpdateNextButtonState()
    {
        // Only enable the next button if the DevDriveManager has a DevDrive selected to apply the configuration to.
        // and if the DevDriveManager has finished loading the dev drives.
        // CanGoToNextPage = DevDriveManagerObj.DevDriveSetupItem != null;
        _setupFlowOrchestrator.NotifyNavigationCanExecuteChanged();
        SyncDevDrivesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDrives from all providers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDrives()
    {
        // We need to run this on a background thread so we don't block the UI thread.
        Task.Run(async () =>
        {
            await _dispatcher.EnqueueAsync(async () =>
            {
                // Remove any existing DevDrivesListViewModels from the list if they exist. E.g when sync button is
                // pressed.
                RemoveDevDrivesListViewModels();

                // Disable the sync and next buttons while we're getting the dev drives.
                DevDriveLoadingCompleted = false;
                UpdateNextButtonState();

                // load the dev drives so we can show them in the UI.
                await LoadAllDevDrivesInTheUI();

                // Enable the sync and next buttons when we're done getting the dev drives.
                UpdateNextButtonState();

                DevDrivesCollectionView.Refresh();
            });
        });
    }

    /// <summary>
    /// Removes all DevDrivesListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDrivesListViewModels()
    {
        var totalLists = _devDriveViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveViewModelList[i].SelectedItem = null;

            // _devDriveViewModelList[i].RemoveCardViewModelEventHandlers();
            DevDriveProviderComboBoxNames.Remove(_devDriveViewModelList[i].DisplayName);
            _devDriveViewModelList.RemoveAt(i);
        }

        // Reset the filter text and the selected provider name.
        DevDriveFilterText = string.Empty;
        SelectedDevDriveProviderComboBoxName = _allKeyWordLocalized;

        // DevDriveManagerObj.DevDriveSetupItem = null;
        ShouldShowCollectionView = false;
        DevDrivesCollectionView.Refresh();
        ProviderComboBoxNamesCollectionView.Refresh();
    }

    /// <summary>
    /// Adds a DevDrivesListViewModel from the DevDriveManager.
    /// </summary>
    private void AddListViewModelToList(DevDrivesListViewModel listViewModel)
    {
        // Add provider name to combo box list.
        if (!DevDriveProviderComboBoxNames.Contains(listViewModel.DisplayName))
        {
            DevDriveProviderComboBoxNames.Add(listViewModel.DisplayName);
        }

        // listViewModel doesn't exist so add it to the list.
        _devDriveViewModelList.Add(listViewModel);
        ShouldShowCollectionView = true;
    }

    /// <summary>
    /// Loads all the DevDrives from all providers and updates the UI with the results.
    /// </summary>
    public async Task LoadAllDevDrivesInTheUI()
    {
        try
        {
            // DevDriveManagerObj.GetDevDrivesAsync(UpdateListViewModelList);
            var existingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            await UpdateListViewModelList(existingDevDrives);
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowList = false;
    }

    private void RemoveSelectedItemIfNotInUI(DevDrivesListViewModel listViewModel)
    {
        if (listViewModel.SelectedItem != null)
        {
            // If the user had a DevDriveCardViewModel selected and then they filtered
            // the list so that the DevDriveCardViewModel doesn't show up in the UI anymore,
            // we need to clear the DevDriveManager's DevDriveSetupItem property.
            // DevDriveManagerObj.DevDriveSetupItem = null;
            listViewModel.SelectedItem = null;
            UpdateNextButtonState();
        }
    }

    public async Task UpdateListViewModelList(IEnumerable<IDevDrive> existingDevDrives)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            var curListViewModel = new DevDrivesListViewModel(/*existingDevDrives*/);

            foreach (var existingDevDrive in existingDevDrives)
            {
                var card = await _devDriveViewModelFactory.CreateCardViewModel(DevDriveManagerObj, existingDevDrive);
                curListViewModel.DevDriveCardCollection.Add(card);
            }

            AddListViewModelToList(curListViewModel);
            DevDriveLoadingCompleted = true;
            ShouldShowShimmerBelowList = true;
        });
    }

    /// <summary>
    /// Sorts the list of DevDrivesListViewModels based on the selected sort option.
    /// </summary>
    /// <param name="index">The current index of the combo box the user selected</param>
    [RelayCommand]
    public void SortComboBoxChanged(int index)
    {
        var direction = index == 0 ? SortDirection.Ascending : SortDirection.Descending;
        DevDrivesCollectionView.SortDescriptions.Clear();
        DevDrivesCollectionView.SortDescriptions.Add(new SortDescription(SortByDisplayName, direction));
    }
}
