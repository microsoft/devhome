// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class StorePackageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _productId;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _publisher;

    [ObservableProperty]
    private string _packageFamilyName;

    public StorePackageViewModel(string productId, string title, string publisher, string packageFamilyName)
    {
        _productId = productId;
        _title = title;
        _publisher = publisher;
        _packageFamilyName = packageFamilyName;
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new(linkString));
    }
}
