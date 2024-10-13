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
using Serilog;
using WindowsSystem = Windows.System;

namespace DevHome.Common.ViewModels;

/// <summary>
/// Used for pages that allow extensions to be installed directly from the page
/// instead of through the extension library page.
/// </summary>
public partial class ExtensionInstallationExpanderViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionInstallationExpanderViewModel));

    private readonly IExtensionService _extensionService;

    private readonly IMicrosoftStoreService _microsoftStoreService;

    private readonly DispatcherQueue _dispatcherQueue;

    public event EventHandler? ExtensionChangedEvent;

    private ObservableCollection<StorePackageViewModel> StorePackages { get; } = new();

    public ObservableCollection<StorePackageViewModel> StorePackagesThatSupportEnvironments { get; } = new();

    [ObservableProperty]
    private bool _isLoadingExtensionData;

    public ExtensionInstallationExpanderViewModel(
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

        if (args.ChangedEventKind == PackageChangedEventKind.Uninstalled)
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
        StorePackages.Clear();
        StorePackagesThatSupportEnvironments.Clear();
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

                var curStorePackage = new StorePackageViewModel(product, _dispatcherQueue, _microsoftStoreService);
                StorePackages.Add(curStorePackage);
                if (curStorePackage.SupportsProviderType("ComputeSystem"))
                {
                    StorePackagesThatSupportEnvironments.Add(curStorePackage);
                }
            }
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
