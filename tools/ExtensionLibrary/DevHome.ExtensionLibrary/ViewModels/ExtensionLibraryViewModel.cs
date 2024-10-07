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
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;
using static DevHome.Common.Helpers.CommonConstants;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionLibraryViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionLibraryViewModel));

    private const string DevHomeProductId = "9N8MHTPHNGVV";

    private readonly IExtensionService _extensionService;
    private readonly DispatcherQueue _dispatcherQueue;

    // All internal Dev Home extensions that should allow users to enable/disable them, should add
    // their class Ids to this set.
    private readonly HashSet<string> _internalClassIdsToBeShownInExtensionsPage =
    [
        HyperVExtensionClassId,
        WSLExtensionClassId,
    ];

    public ObservableCollection<StorePackageViewModel> StorePackagesList { get; set; }

    public ObservableCollection<InstalledPackageViewModel> InstalledPackagesList { get; set; }

    [ObservableProperty]
    private bool _shouldShowStoreError = false;

    public ExtensionLibraryViewModel(IExtensionService extensionService, DispatcherQueue dispatcherQueue)
    {
        _extensionService = extensionService;
        _dispatcherQueue = dispatcherQueue;

        StorePackagesList = new();
        InstalledPackagesList = new();
    }

    [RelayCommand]
    public async Task GetUpdatesButtonAsync()
    {
        await Windows.System.Launcher.LaunchUriAsync(new("ms-windows-store://downloadsandupdates"));
    }

    [RelayCommand]
    public async Task LoadedAsync()
    {
        await GetInstalledPackagesAndExtensionsAsync();
        GetAvailablePackages();

        if (_extensionService != null)
        {
            _extensionService.OnExtensionsChanged -= OnExtensionsChanged;
            _extensionService.OnExtensionsChanged += OnExtensionsChanged;
        }
    }

    [RelayCommand]
    public void Unloaded()
    {
        if (_extensionService != null)
        {
            _extensionService.OnExtensionsChanged -= OnExtensionsChanged;
        }
    }

    private async void OnExtensionsChanged(object? sender, EventArgs e)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            ShouldShowStoreError = false;
            await GetInstalledPackagesAndExtensionsAsync();
            GetAvailablePackages();
        });
    }

    private async Task GetInstalledPackagesAndExtensionsAsync()
    {
        var extensionWrappers = await _extensionService.GetInstalledExtensionsAsync(true);

        InstalledPackagesList.Clear();

        extensionWrappers = extensionWrappers.OrderBy(extensionWrapper => extensionWrapper.PackageDisplayName);

        foreach (var extensionWrapper in extensionWrappers)
        {
            // Don't show self as an extension unless internal extension is allowed to be enabled/disabled in the
            // extensions page.
            if (Package.Current.Id.FullName == extensionWrapper.PackageFullName &&
                !_internalClassIdsToBeShownInExtensionsPage.Contains(extensionWrapper.ExtensionClassId))
            {
                continue;
            }

            var hasSettingsProvider = extensionWrapper.HasProviderType(ProviderType.Settings);
            var extension = new InstalledExtensionViewModel(extensionWrapper.ExtensionDisplayName, extensionWrapper.ExtensionUniqueId, hasSettingsProvider);

            // Each extension is shown under the package that contains it. Check if we have the package in the list
            // already and if not, create it and add it to the list of packages. Then add the extension to that
            // package's list of extensions.
            var package = InstalledPackagesList.FirstOrDefault(p => p.PackageFamilyName == extensionWrapper.PackageFamilyName);
            if (package == null)
            {
                package = new InstalledPackageViewModel(
                    extensionWrapper.PackageDisplayName,
                    extensionWrapper.Publisher,
                    extensionWrapper.PackageFamilyName,
                    extensionWrapper.InstalledDate,
                    extensionWrapper.Version);
                InstalledPackagesList.Add(package);
            }

            package.InstalledExtensionsList.Add(extension);
        }
    }

    private async void GetAvailablePackages()
    {
        StorePackagesList.Clear();

        var extensionJsonData = await _extensionService.GetExtensionJsonDataAsync();
        if (extensionJsonData == null)
        {
            _log.Error("No package data found");
            ShouldShowStoreError = true;
            return;
        }

        var tempStorePackagesList = new List<StorePackageViewModel>();

        foreach (var product in extensionJsonData.Products)
        {
            // Don't show packages of already installed extensions as available.
            if (IsAlreadyInstalled(product.Properties.PackageFamilyName))
            {
                continue;
            }

            _log.Information($"Found package: {product.ProductId}, {product.Properties.PackageFamilyName}");

            var storePackage = new StorePackageViewModel(product);

            tempStorePackagesList.Add(storePackage);
        }

        tempStorePackagesList = tempStorePackagesList.OrderBy(storePackage => storePackage.Title).ToList();
        foreach (var storePackage in tempStorePackagesList)
        {
            StorePackagesList.Add(storePackage);
        }
    }

    private bool IsAlreadyInstalled(string packageFamilyName) =>
        InstalledPackagesList.Any(package => packageFamilyName == package.PackageFamilyName);

    /// <summary>
    /// Determine whether to show a message that there are no more packages with Dev Home extensions available to
    /// install. This message is shown when there are no extensions in the list and there was no error retrieving the
    /// list from the store data.
    /// </summary>
    public Visibility GetNoAvailablePackagesVisibility(int availablePackagesCount, bool shouldShowStoreError)
    {
        return (availablePackagesCount == 0 && !shouldShowStoreError) ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand]
    public void SendFeedbackClick()
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        _ = navigationService.NavigateTo(KnownPageKeys.Feedback);
    }
}
