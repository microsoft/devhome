// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Windows.Data.Json;
using Windows.Storage;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionLibraryViewModel : ObservableObject
{
    private readonly string devHomeProductId = "9N8MHTPHNGVV";

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<StorePackageViewModel> _storePackagesList = new ();

    [ObservableProperty]
    private bool _shouldShowStoreError = false;

    public ExtensionLibraryViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        var pluginService = Application.Current.GetService<IPluginService>();
        pluginService.OnPluginsChanged -= OnPluginsChanged;
        pluginService.OnPluginsChanged += OnPluginsChanged;

        GetAvailablePackages();
    }

    [RelayCommand]
    public async Task GetUpdatesButtonAsync()
    {
        await Launcher.LaunchUriAsync(new ("ms-windows-store://downloadsandupdates"));
    }

    private async void OnPluginsChanged(object? sender, EventArgs e)
    {
        await _dispatcher.EnqueueAsync(() =>
        {
            ShouldShowStoreError = false;
            GetAvailablePackages();
        });
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
        // Coming  in next commit.
        return false;
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
