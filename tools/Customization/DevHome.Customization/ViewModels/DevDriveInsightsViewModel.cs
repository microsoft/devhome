// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.Models.Environments;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.Customization.ViewModels;

public partial class DevDriveInsightsViewModel : SetupPageViewModelBase
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ToastNotificationService _toastNotificationService;

    private readonly string _allKeyWordLocalized;

    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly ObservableCollection<DevDrivesListViewModel> _devDriveViewModelList = new();

    private readonly ObservableCollection<DevDriveOptimizersListViewModel> _devDriveOptimizerViewModelList = new();

    private readonly ObservableCollection<DevDriveOptimizedListViewModel> _devDriveOptimizedViewModelList = new();

    private readonly ISetupFlowStringResource _setupFlowStringResource;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly DevDriveViewModelFactory _devDriveViewModelFactory;

    private readonly DevDriveOptimizerViewModelFactory _devDriveOptimizerViewModelFactory;

    private readonly DevDriveOptimizedViewModelFactory _devDriveOptimizedViewModelFactory;

    [ObservableProperty]
    private bool _shouldShowCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizerCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizedCollectionView;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowList;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowOptimizerList;

    [ObservableProperty]
    private bool _shouldShowShimmerBelowOptimizedList;

    [ObservableProperty]
    private ObservableCollection<string> _devDriveProviderComboBoxNames;

    [NotifyPropertyChangedFor(nameof(ProviderComboBoxNamesCollectionView))]
    [NotifyCanExecuteChangedFor(nameof(SyncDevDrivesCommand))]
    [ObservableProperty]
    private bool _devDriveLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizerLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizedLoadingCompleted;

    [ObservableProperty]
    private AdvancedCollectionView _providerComboBoxNamesCollectionView;

    public ObservableCollection<string> DevDrivesSortOptions { get; private set; }

    public AdvancedCollectionView DevDrivesCollectionView { get; private set; }

    public AdvancedCollectionView DevDriveOptimizersCollectionView { get; private set; }

    public AdvancedCollectionView DevDriveOptimizedCollectionView { get; private set; }

    public IDevDriveManager DevDriveManagerObj { get; private set; }

    public string SelectedDevDriveProviderComboBoxName { get; set; }

    private IEnumerable<IDevDrive> ExistingDevDrives { get; set; } = Enumerable.Empty<IDevDrive>();

    public DevDriveInsightsViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowViewModel setupflowModel,
        SetupFlowOrchestrator orchestrator,
        IDevDriveManager devDriveManager,
        DevDriveViewModelFactory devDriveViewModelFactory,
        DevDriveOptimizerViewModelFactory devDriveOptimizerViewModelFactory,
        DevDriveOptimizedViewModelFactory devDriveOptimizedViewModelFactory,
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

        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesOptimizersListViewModels easier.
        DevDriveOptimizersCollectionView = new AdvancedCollectionView(_devDriveOptimizerViewModelList, true);

        // Add AdvancedCollectionView to make filtering and sorting the list of DevDrivesOptimizedListViewModels easier.
        DevDriveOptimizedCollectionView = new AdvancedCollectionView(_devDriveOptimizedViewModelList, true);

        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _devDriveViewModelFactory = devDriveViewModelFactory;
        _devDriveOptimizerViewModelFactory = devDriveOptimizerViewModelFactory;
        _devDriveOptimizedViewModelFactory = devDriveOptimizedViewModelFactory;
        DevDriveManagerObj = devDriveManager;
        _setupFlowViewModel = setupflowModel;

        _toastNotificationService = toastNotificationService;

        _ = this.OnFirstNavigateToAsync();
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
        // FilterTextChangedCommand.Execute(DevDriveFilterText);
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
            if (!shouldShowAllProviders)
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
                if (shouldShowAllProviders)
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
        GetDevDriveOptimizers();
        GetDevDriveOptimizeds();
    }

    public void UpdateNextButtonState()
    {
        // Only enable the next button if the DevDriveManager has a DevDrive selected to apply the configuration to.
        // and if the DevDriveManager has finished loading the dev drives.
        _setupFlowOrchestrator.NotifyNavigationCanExecuteChanged();
        SyncDevDrivesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDriveOptimizers()
    {
        // We need to run this on a background thread so we don't block the UI thread.
        Task.Run(async () =>
        {
            await _dispatcher.EnqueueAsync(async () =>
            {
                // Remove any existing DevDriveOptimizersListViewModels from the list if they exist.
                RemoveDevDriveOptimizersListViewModels();

                // Disable the sync and next buttons while we're getting the dev drives.
                DevDriveOptimizerLoadingCompleted = false;

                // load the dev drives so we can show them in the UI.
                await LoadAllDevDriveOptimizersInTheUI();

                // Enable the sync and next buttons when we're done getting the dev drives.
                UpdateNextButtonState();

                DevDriveOptimizersCollectionView.Refresh();
            });
        });
    }

    /// <summary>
    /// Removes all DevDriveOptimizersListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizersListViewModels()
    {
        var totalLists = _devDriveOptimizerViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveOptimizerViewModelList.RemoveAt(i);
        }

        ShouldShowOptimizerCollectionView = false;
        DevDriveOptimizersCollectionView.Refresh();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizedCards.
    /// </summary>
    private void GetDevDriveOptimizeds()
    {
        // We need to run this on a background thread so we don't block the UI thread.
        Task.Run(async () =>
        {
            await _dispatcher.EnqueueAsync(async () =>
            {
                // Remove any existing DevDriveOptimizedListViewModels from the list if they exist.
                RemoveDevDriveOptimizedListViewModels();

                // Disable the sync and next buttons while we're getting the dev drives.
                DevDriveOptimizedLoadingCompleted = false;

                // load the dev drives so we can show them in the UI.
                await LoadAllDevDriveOptimizedsInTheUI();

                // Enable the sync and next buttons when we're done getting the dev drives.
                UpdateNextButtonState();

                DevDriveOptimizedCollectionView.Refresh();
            });
        });
    }

    /// <summary>
    /// Removes all DevDriveOptimizedListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizedListViewModels()
    {
        var totalLists = _devDriveOptimizedViewModelList.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            _devDriveOptimizedViewModelList.RemoveAt(i);
        }

        ShouldShowOptimizedCollectionView = false;
        DevDriveOptimizedCollectionView.Refresh();
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
            _devDriveViewModelList.RemoveAt(i);
        }

        // Reset the filter text and the selected provider name.
        SelectedDevDriveProviderComboBoxName = _allKeyWordLocalized;
        ShouldShowCollectionView = false;
        DevDrivesCollectionView.Refresh();
        ProviderComboBoxNamesCollectionView.Refresh();
    }

    /// <summary>
    /// Adds a DevDrivesListViewModel from the DevDriveManager.
    /// </summary>
    private void AddListViewModelToList(DevDrivesListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveViewModelList.Add(listViewModel);
        ShouldShowCollectionView = true;
    }

    /// <summary>
    /// Adds a DevDriveOptimizersListViewModel.
    /// </summary>
    private void AddOptimizerListViewModelToList(DevDriveOptimizersListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveOptimizerViewModelList.Add(listViewModel);
        ShouldShowOptimizerCollectionView = true;
    }

    /// <summary>
    /// Adds a DevDriveOptimizersListViewModel.
    /// </summary>
    private void AddOptimizedListViewModelToList(DevDriveOptimizedListViewModel listViewModel)
    {
        // listViewModel doesn't exist so add it to the list.
        _devDriveOptimizedViewModelList.Add(listViewModel);
        ShouldShowOptimizedCollectionView = true;
    }

    /// <summary>
    /// Loads all the DevDrives from all providers and updates the UI with the results.
    /// </summary>
    public async Task LoadAllDevDrivesInTheUI()
    {
        try
        {
            ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            await UpdateListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowList = false;
    }

    /// <summary>
    /// Loads all the DevDriveOptimizers and updates the UI with the results.
    /// </summary>
    public async Task LoadAllDevDriveOptimizersInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            await UpdateOptimizerListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowOptimizerList = false;
    }

    /// <summary>
    /// Loads all the DevDriveOptimizedCards and updates the UI with the results.
    /// </summary>
    public async Task LoadAllDevDriveOptimizedsInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            await UpdateOptimizedListViewModelList();
        }
        catch (Exception /*ex*/)
        {
            // Log.Logger?.ReportError(Log.Component.SetupTarget, $"Error loading DevDriveViewModels data", ex);
        }

        ShouldShowShimmerBelowOptimizedList = false;
    }

    private void RemoveSelectedItemIfNotInUI(DevDrivesListViewModel listViewModel)
    {
        UpdateNextButtonState();
    }

    public async Task UpdateListViewModelList()
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            var curListViewModel = new DevDrivesListViewModel();
            foreach (var existingDevDrive in ExistingDevDrives)
            {
                var card = await _devDriveViewModelFactory.CreateCardViewModel(DevDriveManagerObj, existingDevDrive);
                curListViewModel.DevDriveCardCollection.Add(card);
            }

            AddListViewModelToList(curListViewModel);
            DevDriveLoadingCompleted = true;
            ShouldShowShimmerBelowList = true;
        });
    }

    private string? GetExistingCacheLocation(string rootDirectory, string targetDirectoryName)
    {
        var fullDirectoryPath = rootDirectory + targetDirectoryName;
        if (Directory.Exists(fullDirectoryPath))
        {
            return fullDirectoryPath;
        }
        else
        {
            var subDirPrefix = rootDirectory + "\\Packages\\PythonSoftwareFoundation.Python";
            var subDirectories = Directory.GetDirectories(rootDirectory + "\\Packages", "*", SearchOption.TopDirectoryOnly);
            var matchingSubdirectory = subDirectories.FirstOrDefault(subdir => subdir.StartsWith(subDirPrefix, StringComparison.OrdinalIgnoreCase));
            var alternateFullDirectoryPath = matchingSubdirectory + "\\localcache\\local" + targetDirectoryName;
            if (Directory.Exists(alternateFullDirectoryPath))
            {
                return alternateFullDirectoryPath;
            }
        }

        return null;
    }

    private bool CacheInDevDrive(string existingPipCacheLocation)
    {
        foreach (var existingDrive in ExistingDevDrives)
        {
            if (existingPipCacheLocation.StartsWith(existingDrive.DriveLetter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public async Task UpdateOptimizerListViewModelList()
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            var curOptimizerListViewModel = new DevDriveOptimizersListViewModel();
            var cacheSubDir = "\\pip\\cache";
            var environmentVariable = "PIP_CACHE_DIR";
            var localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var existingPipCacheLocation = GetExistingCacheLocation(localAppDataDir.ToString(), cacheSubDir);
            if (existingPipCacheLocation != null && !CacheInDevDrive(existingPipCacheLocation))
            {
                var card = await _devDriveOptimizerViewModelFactory.CreateOptimizerCardViewModel(
                    "Pip cache (Python)",
                    existingPipCacheLocation,
                    "D:\\packages" + cacheSubDir /*example location on dev drive to move cache to*/,
                    environmentVariable /*environmentVariableToBeSet*/);
                curOptimizerListViewModel.DevDriveOptimizerCardCollection.Add(card);

                AddOptimizerListViewModelToList(curOptimizerListViewModel);
                DevDriveOptimizerLoadingCompleted = true;
                ShouldShowShimmerBelowOptimizerList = true;
            }
        });
    }

    public async Task UpdateOptimizedListViewModelList()
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            var curOptimizerListViewModel = new DevDriveOptimizersListViewModel();
            var environmentVariable = "PIP_CACHE_DIR";

            // We retrieve the cache location from environment variable, because if the cache might have already moved.
            var movedPipCacheLocation = Environment.GetEnvironmentVariable(environmentVariable);
            if (movedPipCacheLocation != null && CacheInDevDrive(movedPipCacheLocation))
            {
                // Cache already in dev drive, show the "Optimized" card
                var curOptimizedListViewModel = new DevDriveOptimizedListViewModel();
                var card = await _devDriveOptimizedViewModelFactory.CreateOptimizedCardViewModel(
                    "Pip cache (Python)",
                    movedPipCacheLocation,
                    environmentVariable);
                curOptimizedListViewModel.DevDriveOptimizedCardCollection.Add(card);

                AddOptimizedListViewModelToList(curOptimizedListViewModel);
                DevDriveOptimizedLoadingCompleted = true;
                ShouldShowShimmerBelowOptimizedList = true;
            }
        });
    }
}
