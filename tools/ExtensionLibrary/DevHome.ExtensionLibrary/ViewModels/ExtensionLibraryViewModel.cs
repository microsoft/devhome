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
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Storage;
using Windows.System;
using WinUIEx;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionLibraryViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionLibraryViewModel));

    private readonly string devHomeProductId = "9N8MHTPHNGVV";

    private readonly IExtensionService _extensionService;
    private readonly WindowEx _windowEx;

    // All internal Dev Home extensions that should allow users to be enable/disable them, should add
    // their class Ids to this set.
    private readonly HashSet<string> _internalClassIdsToBeShownInExtensionsPage = new()
    {
        CommonConstants.HyperVExtensionClassId,
    };

    public ObservableCollection<StorePackageViewModel> StorePackagesList { get; set; }

    public ObservableCollection<InstalledPackageViewModel> InstalledPackagesList { get; set; }

    [ObservableProperty]
    private bool _shouldShowStoreError = false;

    public ExtensionLibraryViewModel(IExtensionService extensionService, WindowEx windowEx)
    {
        _extensionService = extensionService;
        _windowEx = windowEx;

        extensionService.OnExtensionsChanged -= OnExtensionsChanged;
        extensionService.OnExtensionsChanged += OnExtensionsChanged;

        StorePackagesList = new();
        InstalledPackagesList = new();
    }

    [RelayCommand]
    public async Task GetUpdatesButtonAsync()
    {
        await Launcher.LaunchUriAsync(new("ms-windows-store://downloadsandupdates"));
    }

    [RelayCommand]
    public async Task LoadedAsync()
    {
        await GetInstalledExtensionsAsync();
        GetAvailablePackages();
    }

    private async void OnExtensionsChanged(object? sender, EventArgs e)
    {
        await _windowEx.DispatcherQueue.EnqueueAsync(async () =>
        {
            ShouldShowStoreError = false;
            await GetInstalledExtensionsAsync();
            GetAvailablePackages();
        });
    }

    private async Task GetInstalledExtensionsAsync()
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

    private async Task<string> GetStoreData()
    {
        var packagesFileContents = string.Empty;
        var packagesFileName = "extensionResult.json";
        try
        {
            _log.Information($"Get packages file '{packagesFileName}'");
            var uri = new Uri($"ms-appx:///DevHome.ExtensionLibrary/Assets/{packagesFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            packagesFileContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error retrieving packages");
            ShouldShowStoreError = true;
        }

        return packagesFileContents;
    }

    private async void GetAvailablePackages()
    {
        StorePackagesList.Clear();

        var storeData = await GetStoreData();
        if (string.IsNullOrEmpty(storeData))
        {
            _log.Error("No package data found");
            ShouldShowStoreError = true;
            return;
        }

        var tempStorePackagesList = new List<StorePackageViewModel>();

        var jsonObj = JsonObject.Parse(storeData);
        if (jsonObj != null)
        {
            var products = jsonObj.GetNamedArray("Products");
            foreach (var product in products)
            {
                var productObj = product.GetObject();
                var productId = productObj.GetNamedString("ProductId");

                // Don't show self as available.
                if (productId == devHomeProductId)
                {
                    continue;
                }

                var title = string.Empty;
                var publisher = string.Empty;

                var localizedProperties = productObj.GetNamedArray("LocalizedProperties");
                foreach (var localizedProperty in localizedProperties)
                {
                    var propertyObject = localizedProperty.GetObject();
                    title = propertyObject.GetNamedValue("ProductTitle").GetString();
                    publisher = propertyObject.GetNamedValue("PublisherName").GetString();
                }

                var properties = productObj.GetNamedObject("Properties");
                var packageFamilyName = properties.GetNamedString("PackageFamilyName");

                // Don't show packages of already installed extensions as available.
                if (IsAlreadyInstalled(packageFamilyName))
                {
                    continue;
                }

                _log.Information($"Found package: {productId}, {packageFamilyName}");
                var storePackage = new StorePackageViewModel(productId, title, publisher, packageFamilyName);
                tempStorePackagesList.Add(storePackage);
            }
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
