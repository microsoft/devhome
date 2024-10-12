// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Services.Core.Contracts;
using Microsoft.UI.Dispatching;
using WindowsSystem = Windows.System;

namespace DevHome.Common.ViewModels;

public partial class ExtensionInstallationViewModel : ObservableObject
{
    private readonly IExtensionService _extensionService;

    private readonly IMicrosoftStoreService _microsoftStoreService;

    private readonly DispatcherQueue _dispatcherQueue;

    public event EventHandler? ExtensionChangedEvent;

    [ObservableProperty]
    private ObservableCollection<StorePackageViewModel> _storePackages = new();

    [ObservableProperty]
    private ObservableCollection<StorePackageViewModel> _storePackagesThatSupportEnvironments = new();

    [ObservableProperty]
    private bool _isLoadingExtensionData;

    public ExtensionInstallationViewModel(
        IMicrosoftStoreService microsoftStoreService,
        DispatcherQueue dispatcherQueue,
        IExtensionService extensionService)
    {
        _extensionService = extensionService;
        _microsoftStoreService = microsoftStoreService;
        _dispatcherQueue = dispatcherQueue;

        var extensionServiceWeakRef = new WeakEventListener<IExtensionService, object?, ExtensionPackageChangedEventArgs>(extensionService)
        {
            OnEventAction = (instance, source, args) => OnExtensionsChanged(instance, args),
            OnDetachAction = (weakEventListener) => extensionService.OnExtensionsChanged -= weakEventListener.OnEvent,
        };

        extensionService.OnExtensionsChanged += extensionServiceWeakRef.OnEvent;
    }

    public void OnExtensionsChanged(object? sender, ExtensionPackageChangedEventArgs args)
    {
        ExtensionChangedEvent?.Invoke(null, args);

        if (args.ChangedEventKind == PackageChangedEventKind.UnInstalled)
        {
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await UpdateExtensionPackageInfoAsync();
            });
        }
    }

    public async Task UpdateExtensionPackageInfoAsync()
    {
        IsLoadingExtensionData = true;
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

                storePackages.Add(new(product, _dispatcherQueue, _microsoftStoreService));
            }

            StorePackages = new(storePackages);
            StorePackagesThatSupportEnvironments = new(StorePackages.Where(package => package.EnvironmentProviderDisplayNames.Count > 0));
        }

        IsLoadingExtensionData = false;
    }

    [RelayCommand]
    public async Task BrowseStoreButton()
    {
        var linkString = $"ms-windows-store://search/?query=Dev Home";
        await WindowsSystem.Launcher.LaunchUriAsync(new(linkString));
    }
}
