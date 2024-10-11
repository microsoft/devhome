// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services.Core.Contracts;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.System;

namespace DevHome.Common.ViewModels;

public partial class ExtensionInstallationViewModel : ObservableObject
{
    private readonly IExtensionService _extensionService;

    private readonly IMicrosoftStoreService _microsoftStoreService;

    [ObservableProperty]
    private ObservableCollection<StorePackageViewModel> _storePackages = new();

    [ObservableProperty]
    private ObservableCollection<StorePackageViewModel> _storePackagesThatSupportEnvironments = new();

    public ExtensionInstallationViewModel(
        IMicrosoftStoreService microsoftStoreService,
        IExtensionService extensionService)
    {
        _extensionService = extensionService;
        _microsoftStoreService = microsoftStoreService;
    }

    public async Task UpdateExtensionPackageInfoAsync()
    {
        var extensionData = await Task.Run(_extensionService.GetExtensionJsonDataAsync);
        if (extensionData != null)
        {
            var storePackages = new List<StorePackageViewModel>();
            foreach (var product in extensionData.Products)
            {
                if (_extensionService.IsExtensionInstalled(product.Properties.PackageFamilyName))
                {
                    continue;
                }

                storePackages.Add(new(product, _microsoftStoreService));
            }

            StorePackages = new(storePackages);
            StorePackagesThatSupportEnvironments = new(StorePackages.Where(package => package.EnvironmentProviderDisplayNames.Count > 0));
        }
    }

    [RelayCommand]
    public async Task BrowseStoreButton()
    {
        var linkString = $"ms-windows-store://search/?query=Dev Home";
        await Launcher.LaunchUriAsync(new(linkString));
    }
}
