// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Storage;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionLibraryViewModel : ObservableObject
{
    private const string _hideExtensionsBannerKey = "HideExtensionsBanner";

    private readonly string devHomeProductId = "9N8MHTPHNGVV";

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly IPluginService _pluginService;

    public ObservableCollection<StorePackageViewModel> StorePackagesList { get; set; }

    public ObservableCollection<InstalledPackageViewModel> InstalledPackagesList { get; set; }

    [ObservableProperty]
    private bool _showExtensionsBanner;

    [ObservableProperty]
    private bool _shouldShowStoreError = false;

    public ExtensionLibraryViewModel(IPluginService pluginService)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _pluginService = pluginService;

        pluginService.OnPluginsChanged -= OnPluginsChanged;
        pluginService.OnPluginsChanged += OnPluginsChanged;

        StorePackagesList = new ();
        InstalledPackagesList = new ();

        ShowExtensionsBanner = ShouldShowExtensionsBanner();
    }

    [RelayCommand]
    private async Task ExtensionsBannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new ("https://learn.microsoft.com/en-us/windows/dev-home/extensions"));
    }

    [RelayCommand]
    private void HideExtensionsBannerButton()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        roamingProperties[_hideExtensionsBannerKey] = bool.TrueString;
        ShowExtensionsBanner = false;
    }

    private bool ShouldShowExtensionsBanner()
    {
        var show = true;
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        if (roamingProperties.ContainsKey(_hideExtensionsBannerKey))
        {
            show = false;
        }

        return show;
    }

    [RelayCommand]
    public async Task GetUpdatesButtonAsync()
    {
        await Launcher.LaunchUriAsync(new ("ms-windows-store://downloadsandupdates"));
    }

    [RelayCommand]
    public async Task LoadedAsync()
    {
        await GetInstalledExtensionsAsync();
        GetAvailablePackages();
    }

    private async void OnPluginsChanged(object? sender, EventArgs e)
    {
        await _dispatcher.EnqueueAsync(async () =>
        {
            ShouldShowStoreError = false;
            await GetInstalledExtensionsAsync();
            GetAvailablePackages();
        });
    }

    private async Task GetInstalledExtensionsAsync()
    {
        var extensionWrappers = await _pluginService.GetInstalledPluginsAsync(true);

        InstalledPackagesList.Clear();

        foreach (var extensionWrapper in extensionWrappers)
        {
            // Don't show self as an extension.
            if (Package.Current.Id.FullName == extensionWrapper.PackageFullName)
            {
                continue;
            }

            var hasSettingsProvider = extensionWrapper.HasProviderType(ProviderType.Settings);
            var extension = new InstalledExtensionViewModel(extensionWrapper.Name, extensionWrapper.ExtensionUniqueId, hasSettingsProvider);

            // Each extension is shown under the package that contains it. Check if we have the package in the list
            // already and if not, create it and add it to the list of packages. Then add the extension to that
            // package's list of extensions.
            var package = InstalledPackagesList.FirstOrDefault(p => p.PackageFamilyName == extensionWrapper.PackageFamilyName);
            if (package == null)
            {
                package = new InstalledPackageViewModel(
                    extensionWrapper.Name,
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
            Log.Logger()?.ReportInfo("ExtensionLibraryViewModel", $"Get packages file '{packagesFileName}'");
            var uri = new Uri($"ms-appx:///DevHome.ExtensionLibrary/Assets/{packagesFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            packagesFileContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError("ExtensionLibraryViewModel", "Error retrieving packages", ex);
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
            Log.Logger()?.ReportError("ExtensionLibraryViewModel", "No package data found");
            ShouldShowStoreError = true;
            return;
        }

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

                Log.Logger()?.ReportError("ExtensionLibraryViewModel", $"Found package: {productId}, {packageFamilyName}");
                var storePackage = new StorePackageViewModel(productId, title, publisher, packageFamilyName);
                StorePackagesList.Add(storePackage);
            }
        }
    }

    private bool IsAlreadyInstalled(string packageFamilyName)
    {
        // PackageFullName = Microsoft.Windows.DevHome.Dev_0.0.0.0_x64__8wekyb3d8bbwe
        // PackageFamilyName = Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe
        return InstalledPackagesList.Any(package => packageFamilyName == package.PackageFamilyName);
    }

    /// <summary>
    /// Determine whether to show a message that there are no more packages with Dev Home extensions available to
    /// install. This message is shown when there are no extensions in the list and there was no error retrieving the
    /// list from the store data.
    /// </summary>
    public Visibility GetNoAvailablePackagesVisibility(int availablePackagesCount, bool shouldShowStoreError)
    {
        if (availablePackagesCount == 0 && !shouldShowStoreError)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    [RelayCommand]
    public void SendFeedbackClick()
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        _ = navigationService.NavigateTo(typeof(FeedbackViewModel).FullName!);
    }
}
