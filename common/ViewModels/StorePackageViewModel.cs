// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using DevHome.Common.Models.ExtensionJsonData;
using DevHome.Common.Services;
using DevHome.Services.Core.Contracts;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store.Preview.InstallControl;
using Windows.System;

namespace DevHome.Common.ViewModels;

public partial class StorePackageViewModel : ObservableObject
{
    private readonly Product _product;

    private readonly IMicrosoftStoreService _microsoftStoreService;

    public string ProductId { get; }

    public string Title { get; }

    public string Publisher { get; }

    public string Description { get; }

    public string PackageFamilyName { get; }

    public string AutomationInstallPfn { get; }

    public BitmapSource? IconSource { get; }

    public Dictionary<string, List<string>> SupportedProviderInfo { get; private set; } = new();

    public List<string> SupportedProviderTypesInPackage { get; private set; } = new();

    public List<string> EnvironmentProviderDisplayNames { get; }

    public string? InstallationErrorMessage { get; private set; }

    public string CurrentButtonContentKey { get; private set; } = GetButtonResourceKey;

    public bool IsPackageInstalling { get; private set; }

    public bool IsPackageInstalled { get; private set; }

    private const string GetButtonResourceKey = "GetStorePackageButton";

    private const string PackageInstalledResourceKey = "PackageInstalled";

    private const string RetryPackageInstallationKey = "RetryPackageInstallation";

    public StorePackageViewModel(Product product, IMicrosoftStoreService microsoftStoreService)
    {
        _microsoftStoreService = microsoftStoreService;

        var packageInstalledWeakRef = new WeakEventListener<IMicrosoftStoreService, object, AppInstallManagerItemEventArgs>(microsoftStoreService)
        {
            OnEventAction = (instance, source, args) => PackagedInstallChanged(instance, args),
            OnDetachAction = (weakEventListener) => microsoftStoreService.ItemStatusChanged -= weakEventListener.OnEvent,
        };

        microsoftStoreService.ItemStatusChanged += packageInstalledWeakRef.OnEvent;

        _product = product;
        ProductId = product.ProductId;
        Title = product.Properties.ProductTitle;
        Publisher = product.Properties.PublisherName;
        PackageFamilyName = product.Properties.PackageFamilyName;
        Description = product.Properties.Description;
        AutomationInstallPfn = $"Install_{PackageFamilyName}";
        UpdateSupportedProviderInfo();
        UpdateSupportedProviderTypesInPackageList();
        EnvironmentProviderDisplayNames = GetSupportedProviderDisplayNamesBasedOnType("ComputeSystem");
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        InstallationErrorMessage = null;

        if (IsPackageInstalling)
        {
            return;
        }

        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new(linkString));
    }

    private List<string> GetSupportedProviderDisplayNamesBasedOnType(string providerType)
    {
        if (SupportedProviderInfo.TryGetValue(providerType, out var providerDisplayNameList))
        {
            return providerDisplayNameList;
        }

        return new();
    }

    private void UpdateSupportedProviderTypesInPackageList()
    {
        var supportedProviderTypes = new List<string>();
        foreach (var extension in _product.Properties.DevHomeExtensions)
        {
            supportedProviderTypes.AddRange(extension.SupportedProviderTypes);
        }

        var distinctTypes = new HashSet<string>(supportedProviderTypes).ToList();
        if (_product.Properties.SupportsWidgets)
        {
            distinctTypes.Add("Widgets");
        }

        distinctTypes.Sort();
        SupportedProviderTypesInPackage = distinctTypes;
    }

    private void UpdateSupportedProviderInfo()
    {
        var supportedProviderDisplayNames = new Dictionary<string, List<string>>();
        foreach (var extension in _product.Properties.DevHomeExtensions)
        {
            foreach (var provider in extension.ProviderSpecificProperties)
            {
                if (supportedProviderDisplayNames.TryGetValue(provider.ProviderType, out var providerList))
                {
                    providerList.Add(provider.LocalizedProperties.DisplayName);
                }
                else
                {
                    supportedProviderDisplayNames[provider.ProviderType] = new() { provider.LocalizedProperties.DisplayName };
                }
            }
        }

        // Sort the display name lists so they don't need to be sorted later.
        foreach (var providerType in supportedProviderDisplayNames.Keys)
        {
            supportedProviderDisplayNames[providerType].Sort();
        }

        SupportedProviderInfo = supportedProviderDisplayNames;
    }

    public void PackagedInstallChanged(object sender, AppInstallManagerItemEventArgs args)
    {
        if (!PackageFamilyName.Equals(args.Item.PackageFamilyName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        InstallationErrorMessage = null;
        IsPackageInstalling = true;
        var status = args.Item.GetCurrentStatus();
        var isInstallationDone = true;

        switch (status.InstallState)
        {
            case AppInstallState.Error:
                InstallationErrorMessage = $"Failed. Error code: {status.ErrorCode:X}";
                CurrentButtonContentKey = PackageInstalledResourceKey;
                break;
            case AppInstallState.Canceled:
                CurrentButtonContentKey = GetButtonResourceKey;
                break;
            case AppInstallState.Completed:
                CurrentButtonContentKey = PackageInstalledResourceKey;
                break;
            default:
                isInstallationDone = false;
                break;
        }

        if (isInstallationDone)
        {
            IsPackageInstalling = false;
            IsPackageInstalled = true;
        }
    }
}
