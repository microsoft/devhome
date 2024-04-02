// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels.DevDriveInsights;
using DevHome.Customization.Views;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class DevDriveInsightsViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    public ObservableCollection<DevDriveCardViewModel> DevDriveCardCollection { get; private set; } = new();

    public ObservableCollection<DevDriveOptimizerCardViewModel> DevDriveOptimizerCardCollection { get; private set; } = new();

    public ObservableCollection<DevDriveOptimizedCardViewModel> DevDriveOptimizedCardCollection { get; private set; } = new();

    private readonly OptimizeDevDriveDialogViewModelFactory _optimizeDevDriveDialogViewModelFactory;

    [ObservableProperty]
    private bool _shouldShowCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizerCollectionView;

    [ObservableProperty]
    private bool _shouldShowOptimizedCollectionView;

    [ObservableProperty]
    private bool _devDriveLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizerLoadingCompleted;

    [ObservableProperty]
    private bool _devDriveOptimizedLoadingCompleted;

    public IDevDriveManager DevDriveManagerObj { get; private set; }

    private IEnumerable<IDevDrive> ExistingDevDrives { get; set; } = Enumerable.Empty<IDevDrive>();

    public DevDriveInsightsViewModel(IDevDriveManager devDriveManager, OptimizeDevDriveDialogViewModelFactory optimizeDevDriveDialogViewModelFactory)
    {
        _optimizeDevDriveDialogViewModelFactory = optimizeDevDriveDialogViewModelFactory;
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        DevDriveManagerObj = devDriveManager;
    }

    /// <summary>
    /// Make sure we only get the list of DevDrives from the DevDriveManager once when the page is first navigated to.
    /// All other times will be through the use of the sync button.
    /// </summary>
    public void OnFirstNavigateTo()
    {
        GetDevDrives();
        GetDevDriveOptimizers();
        GetDevDriveOptimizeds();
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDriveOptimizers()
    {
        // Remove any existing DevDriveOptimizersListViewModels from the list if they exist.
        RemoveDevDriveOptimizersListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveOptimizerLoadingCompleted = false;

        // load the dev drives so we can show them in the UI.
        LoadAllDevDriveOptimizersInTheUI();
    }

    /// <summary>
    /// Removes all DevDriveOptimizersListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizersListViewModels()
    {
        var totalLists = DevDriveOptimizerCardCollection.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            DevDriveOptimizerCardCollection.RemoveAt(i);
        }

        ShouldShowOptimizerCollectionView = false;
    }

    /// <summary>
    /// Starts the process of getting the list of DevDriveOptimizedCards.
    /// </summary>
    private void GetDevDriveOptimizeds()
    {
        // Remove any existing DevDriveOptimizedListViewModels from the list if they exist.
        RemoveDevDriveOptimizedListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveOptimizedLoadingCompleted = false;

        // load the dev drives so we can show them in the UI.
        LoadAllDevDriveOptimizedsInTheUI();
    }

    /// <summary>
    /// Removes all DevDriveOptimizedListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDriveOptimizedListViewModels()
    {
        var totalLists = DevDriveOptimizedCardCollection.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            DevDriveOptimizedCardCollection.RemoveAt(i);
        }

        ShouldShowOptimizedCollectionView = false;
    }

    /// <summary>
    /// Starts the process of getting the list of DevDrives from all providers. the sync and next
    /// buttons should be disabled when work is being done.
    /// </summary>
    private void GetDevDrives()
    {
        // Remove any existing DevDrivesListViewModels from the list if they exist. E.g when sync button is
        // pressed.
        RemoveDevDrivesListViewModels();

        // Disable the sync and next buttons while we're getting the dev drives.
        DevDriveLoadingCompleted = false;

        // load the dev drives so we can show them in the UI.
        LoadAllDevDrivesInTheUI();
    }

    /// <summary>
    /// Removes all DevDrivesListViewModels from the list view model list and removes the dev drive
    /// selected to apply the configuration to. This should refresh the UI to show no dev drives.
    /// </summary>
    private void RemoveDevDrivesListViewModels()
    {
        var totalLists = DevDriveCardCollection.Count;
        for (var i = totalLists - 1; i >= 0; i--)
        {
            DevDriveCardCollection.RemoveAt(i);
        }

        // Reset the filter text and the selected provider name.
        ShouldShowCollectionView = false;
    }

    /// <summary>
    /// Loads all the DevDrives from all providers and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDrivesInTheUI()
    {
        try
        {
            ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            UpdateListViewModelList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading Dev Drives data. Error: {ex}");
        }
    }

    /// <summary>
    /// Loads all the DevDriveOptimizers and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDriveOptimizersInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            UpdateOptimizerListViewModelList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading Dev Drive Optimizers data. Error: {ex}");
        }
    }

    /// <summary>
    /// Loads all the DevDriveOptimizedCards and updates the UI with the results.
    /// </summary>
    public void LoadAllDevDriveOptimizedsInTheUI()
    {
        try
        {
            if (!ExistingDevDrives.Any())
            {
                ExistingDevDrives = DevDriveManagerObj.GetAllDevDrivesThatExistOnSystem();
            }

            UpdateOptimizedListViewModelList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading Dev Drive Optimized data. Error: {ex}");
        }
    }

    public void UpdateListViewModelList()
    {
        foreach (var existingDevDrive in ExistingDevDrives)
        {
            DevDriveCardCollection.Add(new DevDriveCardViewModel(existingDevDrive, DevDriveManagerObj));
        }

        DevDriveLoadingCompleted = true;
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

    public void UpdateOptimizerListViewModelList()
    {
        var cacheSubDir = "\\pip\\cache";
        var environmentVariable = "PIP_CACHE_DIR";
        var localAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var existingPipCacheLocation = GetExistingCacheLocation(localAppDataDir.ToString(), cacheSubDir);
        if (existingPipCacheLocation != null && !CacheInDevDrive(existingPipCacheLocation))
        {
            var card = new DevDriveOptimizerCardViewModel(
                _optimizeDevDriveDialogViewModelFactory,
                "Pip cache (Python)",
                existingPipCacheLocation,
                "D:\\packages" + cacheSubDir /*example location on dev drive to move cache to*/,
                environmentVariable /*environmentVariableToBeSet*/);
            DevDriveOptimizerCardCollection.Add(card);
            DevDriveOptimizerLoadingCompleted = true;
        }
    }

    public void UpdateOptimizedListViewModelList()
    {
        var environmentVariable = "PIP_CACHE_DIR";

        // We retrieve the cache location from environment variable, because if the cache might have already moved.
        var movedPipCacheLocation = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(movedPipCacheLocation) && CacheInDevDrive(movedPipCacheLocation))
        {
            // Cache already in dev drive, show the "Optimized" card
            var card = new DevDriveOptimizedCardViewModel("Pip cache (Python)", movedPipCacheLocation, environmentVariable);
            DevDriveOptimizedCardCollection.Add(card);
            DevDriveOptimizedLoadingCompleted = true;
        }
    }
}
