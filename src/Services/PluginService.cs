// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Models;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;

namespace DevHome.Services;

public class ExtensionService : IExtensionService
{
    public event EventHandler OnExtensionsChanged = (_, _) => { };

    private static readonly PackageCatalog _catalog = PackageCatalog.OpenForCurrentUser();
    private static readonly object _lock = new ();

#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IExtensionWrapper> _installedExtensions = new ();
    private static List<IExtensionWrapper> _enabledExtensions = new ();
#pragma warning restore IDE0044 // Add readonly modifier

    public ExtensionService()
    {
        _catalog.PackageInstalling += Catalog_PackageInstalling;
        _catalog.PackageUninstalling += Catalog_PackageUninstalling;
        _catalog.PackageUpdating += Catalog_PackageUpdating;
    }

    private void Catalog_PackageInstalling(PackageCatalog sender, PackageInstallingEventArgs args)
    {
        if (args.IsComplete)
        {
            lock (_lock)
            {
                var isDevHomeExtension = Task.Run(() =>
                {
                    return IsValidDevHomeExtension(args.Package);
                }).Result;

                if (isDevHomeExtension)
                {
                    OnPackageChange(args.Package);
                }
            }
        }
    }

    private void Catalog_PackageUninstalling(PackageCatalog sender, PackageUninstallingEventArgs args)
    {
        if (args.IsComplete)
        {
            lock (_lock)
            {
                foreach (var plugin in _installedExtensions)
                {
                    if (plugin.PackageFullName == args.Package.Id.FullName)
                    {
                        OnPackageChange(args.Package);
                        break;
                    }
                }
            }
        }
    }

    private void Catalog_PackageUpdating(PackageCatalog sender, PackageUpdatingEventArgs args)
    {
        if (args.IsComplete)
        {
            lock (_lock)
            {
                var isDevHomeExtension = Task.Run(() =>
                {
                    return IsValidDevHomeExtension(args.TargetPackage);
                }).Result;

                if (isDevHomeExtension)
                {
                    OnPackageChange(args.TargetPackage);
                }
            }
        }
    }

    private void OnPackageChange(Package package)
    {
        _installedExtensions.Clear();
        _enabledExtensions.Clear();
        OnExtensionsChanged.Invoke(this, EventArgs.Empty);
    }

    private async Task<bool> IsValidDevHomeExtension(Package package)
    {
        var extensions = await AppExtensionCatalog.Open("com.microsoft.devhome").FindAllAsync();
        foreach (var extension in extensions)
        {
            if (package.Id.FullName == extension.Package.Id.FullName)
            {
                var (devHomeProvider, classId) = await GetDevHomeExtensionPropertiesAsync(extension);
                return devHomeProvider != null && classId != null;
            }
        }

        return false;
    }

    private async Task<(IPropertySet?, string?)> GetDevHomeExtensionPropertiesAsync(AppExtension extension)
    {
        var properties = await extension.GetExtensionPropertiesAsync();

        var devHomeProvider = GetSubPropertySet(properties, "DevHomeProvider");
        if (devHomeProvider is null)
        {
            return (null, null);
        }

        var activation = GetSubPropertySet(devHomeProvider, "Activation");
        if (activation is null)
        {
            return (devHomeProvider, null);
        }

        var comActivation = GetSubPropertySet(activation, "CreateInstance");
        if (comActivation is null)
        {
            return (devHomeProvider, null);
        }

        var classId = GetProperty(comActivation, "@ClassId");
        if (classId is null)
        {
            return (devHomeProvider, null);
        }

        return (devHomeProvider, classId);
    }

    public async Task<IEnumerable<AppExtension>> GetInstalledAppExtensionsAsync()
    {
        return await AppExtensionCatalog.Open("com.microsoft.devhome").FindAllAsync();
    }

    public async Task<IEnumerable<IExtensionWrapper>> GetInstalledExtensionsAsync(bool includeDisabledExtensions = false)
    {
        if (_installedExtensions.Count == 0)
        {
            var extensions = await GetInstalledAppExtensionsAsync();
            foreach (var extension in extensions)
            {
                var (devHomeProvider, classId) = await GetDevHomeExtensionPropertiesAsync(extension);
                if (devHomeProvider == null || classId == null)
                {
                    continue;
                }

                var name = extension.DisplayName;
                var pluginWrapper = new ExtensionWrapper(name, extension.Package.Id.FullName, classId);

                var supportedInterfaces = GetSubPropertySet(devHomeProvider, "SupportedInterfaces");
                if (supportedInterfaces is not null)
                {
                    foreach (var supportedInterface in supportedInterfaces)
                    {
                        ProviderType pt;
                        if (Enum.TryParse<ProviderType>(supportedInterface.Key, out pt))
                        {
                            pluginWrapper.AddProviderType(pt);
                        }
                        else
                        {
                            // TODO: throw warning or fire notification that plugin declared unsupported plugin interface
                            // https://github.com/microsoft/devhome/issues/617
                        }
                    }
                }

                var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                var isExtensionDisabled = await localSettingsService.ReadSettingAsync<bool>(extension.Package.Id.FullName + "-ExtensionDisabled");

                _installedExtensions.Add(pluginWrapper);
                if (!isExtensionDisabled)
                {
                    _enabledExtensions.Add(pluginWrapper);
                }
            }
        }

        return includeDisabledExtensions ? _installedExtensions : _enabledExtensions;
    }

    public async Task<IEnumerable<IExtensionWrapper>> StartAllExtensionsAsync()
    {
        var installedExtensions = await GetInstalledExtensionsAsync();
        foreach (var installedExtension in installedExtensions)
        {
            if (!installedExtension.IsRunning())
            {
                await installedExtension.StartExtensionAsync();
            }
        }

        return installedExtensions;
    }

    public async Task SignalStopExtensionsAsync()
    {
        var installedExtensions = await GetInstalledExtensionsAsync();
        foreach (var installedExtension in installedExtensions)
        {
            if (installedExtension.IsRunning())
            {
                installedExtension.SignalDispose();
            }
        }
    }

    public async Task<IEnumerable<IExtensionWrapper>> GetInstalledExtensionsAsync(ProviderType providerType, bool includeDisabledExtensions = false)
    {
        var installedExtensions = await GetInstalledExtensionsAsync(includeDisabledExtensions);

        List<IExtensionWrapper> filteredExtensions = new ();
        foreach (var installedExtension in installedExtensions)
        {
            if (installedExtension.HasProviderType(providerType))
            {
                filteredExtensions.Add(installedExtension);
            }
        }

        return filteredExtensions;
    }

    private IPropertySet? GetSubPropertySet(IPropertySet propSet, string name)
    {
        return propSet[name] as IPropertySet;
    }

    private string? GetProperty(IPropertySet propSet, string name)
    {
        return propSet[name] as string;
    }
}
