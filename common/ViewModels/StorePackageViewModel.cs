// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Models.ExtensionJsonData;
using Windows.System;

namespace DevHome.Common.ViewModels;

public partial class StorePackageViewModel : ObservableObject
{
    private readonly Product _product;

    public string ProductId { get; }

    public string Title { get; }

    public string Publisher { get; }

    public string PackageFamilyName { get; }

    public string AutomationInstallPfn { get; }

    public Dictionary<string, List<string>> SupportedProviderInfo { get; }

    [ObservableProperty]
    private readonly List<string> _supportedProviderTypesInPackage = new();

    public StorePackageViewModel(Product product)
    {
        _product = product;
        ProductId = product.ProductId;
        Title = product.Properties.ProductTitle;
        Publisher = product.Properties.PublisherName;
        PackageFamilyName = product.Properties.PackageFamilyName;
        AutomationInstallPfn = $"Install_{PackageFamilyName}";
        SupportedProviderInfo = GetSupportedProviderInfo();
        SupportedProviderTypesInPackage = GetSupportedProviderTypesInPackage();
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new(linkString));
    }

    public void FilterOutProviderTypes(List<string> providerTypes)
    {
        SupportedProviderTypesInPackage.RemoveAll(providerTypes.Contains);
    }

    public List<string> GetSupportedProviderDisplayNamesBasedOnType(string providerType)
    {
        if (SupportedProviderInfo.TryGetValue(providerType, out var providerDisplayNameList))
        {
            return providerDisplayNameList;
        }

        return new();
    }

    private List<string> GetSupportedProviderTypesInPackage()
    {
        var supportedProviderInfo = SupportedProviderInfo;
        var supportedProviderTypes = supportedProviderInfo.Keys.ToList();
        supportedProviderTypes.Sort();
        return supportedProviderTypes;
    }

    private Dictionary<string, List<string>> GetSupportedProviderInfo()
    {
        var supportedProviderDisplayNames = new Dictionary<string, List<string>>();
        foreach (var extension in _product.Properties.DevHomeExtensions)
        {
            foreach (var provider in extension.ProviderSpecificProperties)
            {
                supportedProviderDisplayNames[provider.ProviderType].Add(provider.LocalizedProperties.DisplayName);
            }
        }

        // Sort the display name lists so they don't need to be sorted later.
        foreach (var providerType in supportedProviderDisplayNames.Keys)
        {
            supportedProviderDisplayNames[providerType].Sort();
        }

        return supportedProviderDisplayNames;
    }
}
