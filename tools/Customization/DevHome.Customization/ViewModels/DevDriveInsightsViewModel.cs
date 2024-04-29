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
using DevHome.Customization.Helpers;
using DevHome.Customization.ViewModels.DevDriveInsights;
using DevHome.Customization.Views;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Serilog;

namespace DevHome.Customization.ViewModels;

public partial class DevDriveInsightsViewModel : ObservableObject
{
    private readonly ShellSettings _shellSettings;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

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

    [ObservableProperty]
    private string _devDriveVolumesText;

    public IDevDriveManager DevDriveManagerObj { get; private set; }

    private IEnumerable<IDevDrive> ExistingDevDrives { get; set; } = Enumerable.Empty<IDevDrive>();

    private static readonly string _appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private static readonly string _localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private static readonly string _userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private const string PackagesStr = "packages";

    private const string CacheStr = "cache";

    private const string ArchivesStr = "archives";

    public DevDriveInsightsViewModel(IDevDriveManager devDriveManager, OptimizeDevDriveDialogViewModelFactory optimizeDevDriveDialogViewModelFactory)
    {
        _shellSettings = new ShellSettings();

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs =
        [
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("DevDriveInsights_Header"), typeof(DevDriveInsightsViewModel).FullName!)
        ];

        _optimizeDevDriveDialogViewModelFactory = optimizeDevDriveDialogViewModelFactory;
        DevDriveManagerObj = devDriveManager;
        DevDriveVolumesText = new(stringResource.GetLocalized("DevDriveVolumes_Header"));
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
            Log.Error(ex, $"Error loading Dev Drives data. Error: {ex}");
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
            Log.Error(ex, $"Error loading Dev Drive Optimizers data. Error: {ex}");
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
            Log.Error(ex, $"Error loading Dev Drive Optimized data. Error: {ex}");
        }
    }

    public void UpdateListViewModelList()
    {
        foreach (var existingDevDrive in ExistingDevDrives)
        {
            DevDriveCardCollection.Add(new DevDriveCardViewModel(existingDevDrive));
        }

        DevDriveLoadingCompleted = true;
    }

    private readonly List<DevDriveCacheData> _cacheInfo =
    [
        new DevDriveCacheData
        {
            CacheName = "Pip cache (Python)",
            EnvironmentVariable = "PIP_CACHE_DIR",
            CacheDirectory = new List<string>
            {
                Path.Join(_localAppDataPath, "pip", CacheStr),
                Path.Join(_localAppDataPath, PackagesStr, "PythonSoftwareFoundation.Python"),
            },
            ExampleSubDirectory = Path.Join(PackagesStr, "pip", CacheStr),
        },
        new DevDriveCacheData
        {
            CacheName = "NuGet cache (dotnet)",
            EnvironmentVariable = "NUGET_PACKAGES",
            CacheDirectory = new List<string> { Path.Join(_userProfilePath, ".nuget", PackagesStr) },
            ExampleSubDirectory = Path.Join(PackagesStr, "NuGet", CacheStr),
        },
        new DevDriveCacheData
        {
            CacheName = "Npm cache (NodeJS)",
            EnvironmentVariable = "NPM_CONFIG_CACHE",
            CacheDirectory = new List<string>
            {
                Path.Join(_appDataPath, "npm-cache"),
                Path.Join(_localAppDataPath, "npm-cache"),
            },
            ExampleSubDirectory = Path.Join(PackagesStr, "npm"),
        },
        new DevDriveCacheData
        {
            CacheName = "Vcpkg cache",
            EnvironmentVariable = "VCPKG_DEFAULT_BINARY_CACHE",
            CacheDirectory = new List<string>
            {
                Path.Join(_appDataPath, "vcpkg", ArchivesStr),
                Path.Join(_localAppDataPath, "vcpkg", ArchivesStr),
            },
            ExampleSubDirectory = Path.Join(PackagesStr, "vcpkg"),
        },
        new DevDriveCacheData
        {
            CacheName = "Cargo cache (Rust)",
            EnvironmentVariable = "CARGO_HOME",
            CacheDirectory = new List<string> { Path.Join(_userProfilePath, ".cargo") },
            ExampleSubDirectory = Path.Join(PackagesStr, "cargo"),
        },
        new DevDriveCacheData
        {
            CacheName = "Maven cache (Java)",
            EnvironmentVariable = "MAVEN_OPTS",
            CacheDirectory = new List<string> { Path.Join(_userProfilePath, ".m2") },
            ExampleSubDirectory = Path.Join(PackagesStr, "m2"),
        },
        new DevDriveCacheData
        {
            CacheName = "Gradle cache (Java)",
            EnvironmentVariable = "GRADLE_USER_HOME",
            CacheDirectory = new List<string> { Path.Join(_userProfilePath, ".gradle") },
            ExampleSubDirectory = Path.Join(PackagesStr, "gradle"),
        }
    ];

    private string? GetExistingCacheLocation(DevDriveCacheData cache)
    {
        foreach (var cacheDirectory in cache.CacheDirectory!)
        {
            if (Directory.Exists(cacheDirectory))
            {
                return cacheDirectory;
            }
            else
            {
                var subDirectories = Directory.GetDirectories(Path.Join(_localAppDataPath, PackagesStr), "*", SearchOption.TopDirectoryOnly);
                var matchingSubdirectory = subDirectories.FirstOrDefault(subdir => subdir.StartsWith(cacheDirectory, StringComparison.OrdinalIgnoreCase));
                if (Directory.Exists(matchingSubdirectory))
                {
                    var actualCacheDirectory = Path.Join(matchingSubdirectory, "LocalCache", "Local", "pip", CacheStr);
                    if (matchingSubdirectory.Contains("PythonSoftwareFoundation") && Directory.Exists(actualCacheDirectory))
                    {
                        return actualCacheDirectory;
                    }
                }
            }
        }

        return null;
    }

    private bool CacheInDevDrive(string existingCacheLocation)
    {
        foreach (var existingDrive in ExistingDevDrives)
        {
            if (existingCacheLocation.StartsWith(existingDrive.DriveLetter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateOptimizerListViewModelList()
    {
        foreach (var cache in _cacheInfo)
        {
            var existingCacheLocation = GetExistingCacheLocation(cache);
            var environmentVariablePath = Environment.GetEnvironmentVariable(cache.EnvironmentVariable!, EnvironmentVariableTarget.User);
            if (environmentVariablePath is not null && CacheInDevDrive(environmentVariablePath!))
            {
                continue;
            }

            if (existingCacheLocation == null || CacheInDevDrive(existingCacheLocation))
            {
                continue;
            }

            List<string> existingDevDriveLetters = ExistingDevDrives.Select(x => x.DriveLetter.ToString()).ToList();

            var exampleDirectory = Path.Join(existingDevDriveLetters[0] + ":", cache.ExampleSubDirectory);
            var card = new DevDriveOptimizerCardViewModel(
                _optimizeDevDriveDialogViewModelFactory,
                cache.CacheName!,
                existingCacheLocation,
                exampleDirectory!, // example location on dev drive to move cache to
                cache.EnvironmentVariable!, // environmentVariableToBeSet
                existingDevDriveLetters,
                !string.IsNullOrEmpty(environmentVariablePath));
            DevDriveOptimizerCardCollection.Add(card);
        }

        DevDriveOptimizerLoadingCompleted = true;
    }

    public void UpdateOptimizedListViewModelList()
    {
        foreach (var cache in _cacheInfo)
        {
            // We retrieve the cache location from environment variable, because if the cache might have already moved.
            var movedCacheLocation = Environment.GetEnvironmentVariable(cache.EnvironmentVariable!, EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(movedCacheLocation) && CacheInDevDrive(movedCacheLocation))
            {
                // Cache already in dev drive, show the "Optimized" card
                var card = new DevDriveOptimizedCardViewModel(cache.CacheName!, movedCacheLocation, cache.EnvironmentVariable!);
                DevDriveOptimizedCardCollection.Add(card);
            }
        }

        DevDriveOptimizedLoadingCompleted = true;
    }
}
