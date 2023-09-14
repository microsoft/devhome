// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Dashboard.Helpers;
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
        Log.Logger()?.ReportInfo("StorePackageViewModel", $"Start of constructor");
        _productId = productId;
        _title = title;
        _publisher = publisher;
        _packageFamilyName = packageFamilyName;
        Log.Logger()?.ReportInfo("StorePackageViewModel", $"End of constructor");
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        Log.Logger()?.ReportInfo("StorePackageViewModel", $"LaunchStoreButton");
        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new (linkString));
    }
}
