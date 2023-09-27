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

public class ExtensionService : IExtensionService, IDisposable
{
    public event EventHandler OnExtensionsChanged = (_, _) => { };

    private static readonly PackageCatalog _catalog = PackageCatalog.OpenForCurrentUser();
    private static readonly object _lock = new ();
    private readonly SemaphoreSlim _getInstalledExtensionsLock = new (1, 1);
    private bool _disposedValue;

#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IExtensionWrapper> _installedExtensions = new ();
    private static List<IExtensionWrapper> _enabledExtensions = new ();
    private static List<string> _installedWidgetsPackageFamilyNames = new ();
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
                foreach (var extension in _installedExtensions)
                {
                    if (extension.PackageFullName == args.Package.Id.FullName)
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
        _installedWidgetsPackageFamilyNames.Clear();
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
        await _getInstalledExtensionsLock.WaitAsync();
        try
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

                    var extensionWrapper = new ExtensionWrapper(extension, classId);

                    var supportedInterfaces = GetSubPropertySet(devHomeProvider, "SupportedInterfaces");
                    if (supportedInterfaces is not null)
                    {
                        foreach (var supportedInterface in supportedInterfaces)
                        {
                            ProviderType pt;
                            if (Enum.TryParse<ProviderType>(supportedInterface.Key, out pt))
                            {
                                extensionWrapper.AddProviderType(pt);
                            }
                            else
                            {
                                // TODO: throw warning or fire notification that extension declared unsupported extension interface
                                // https://github.com/microsoft/devhome/issues/617
                            }
                        }
                    }

                    var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                    var extensionUniqueId = extension.AppInfo.AppUserModelId + "!" + extension.Id;
                    var isExtensionDisabled = await localSettingsService.ReadSettingAsync<bool>(extensionUniqueId + "-ExtensionDisabled");

                    _installedExtensions.Add(extensionWrapper);
                    if (!isExtensionDisabled)
                    {
                        _enabledExtensions.Add(extensionWrapper);
                    }
                }
            }

            return includeDisabledExtensions ? _installedExtensions : _enabledExtensions;
        }
        finally
        {
            _getInstalledExtensionsLock.Release();
        }
    }

    private async Task<IEnumerable<string>> GetInstalledWidgetExtensionsAsync()
    {
        await _getInstalledExtensionsLock.WaitAsync();
        try
        {
            if (_installedWidgetsPackageFamilyNames.Count == 0)
            {
                var widgetExtensions = await AppExtensionCatalog.Open("com.microsoft.windows.widgets").FindAllAsync();
                foreach (var widgetExtension in widgetExtensions)
                {
                    _installedWidgetsPackageFamilyNames.Add(widgetExtension.Package.Id.FamilyName);
                }
            }

            return _installedWidgetsPackageFamilyNames;
        }
        finally
        {
            _getInstalledExtensionsLock.Release();
        }
    }

    public async Task<IEnumerable<string>> GetInstalledDevHomeWidgetPackageFamilyNamesAsync(bool includeDisabledExtensions = false)
    {
        var devHomeExtensionWrappers = await GetInstalledExtensionsAsync(includeDisabledExtensions);
        var widgetExtensionWrappers = await GetInstalledWidgetExtensionsAsync();

        var ids = devHomeExtensionWrappers.Select(x => x.PackageFamilyName).Intersect(widgetExtensionWrappers).ToList();

        return ids;
    }

    public async Task<IEnumerable<IExtensionWrapper>> GetAllExtensionsAsync()
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

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _getInstalledExtensionsLock.Dispose();
            }

            _disposedValue = true;
        }
    }

    private IPropertySet? GetSubPropertySet(IPropertySet propSet, string name)
    {
        return propSet[name] as IPropertySet;
    }

    private string? GetProperty(IPropertySet propSet, string name)
    {
        return propSet[name] as string;
    }

    public void EnableExtension(string extensionUniqueId)
    {
        var extension = _installedExtensions.Where(extension => extension.ExtensionUniqueId == extensionUniqueId);
        _enabledExtensions.Add(extension.First());
    }

    public void DisableExtension(string extensionUniqueId)
    {
        var extension = _enabledExtensions.Where(extension => extension.ExtensionUniqueId == extensionUniqueId);
        _enabledExtensions.Remove(extension.First());
    }
}
