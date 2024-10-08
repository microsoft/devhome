// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Models.ExtensionJsonData;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class StorePackageViewModel : ObservableObject
{
    private readonly Product _product;

    [ObservableProperty]
    private string _productId;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _publisher;

    [ObservableProperty]
    private string _packageFamilyName;

    [ObservableProperty]
    private string _automationInstallPfn;

    public StorePackageViewModel(Product product)
    {
        _product = product;
        _productId = product.ProductId;
        _title = product.Properties.ProductTitle;
        _publisher = product.Properties.PublisherName;
        _packageFamilyName = product.Properties.PackageFamilyName;
        _automationInstallPfn = $"Install_{_packageFamilyName}";
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new(linkString));
    }
}
