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

public class PluginService : IPluginService, IDisposable
{
    public event EventHandler OnPluginsChanged = (_, _) => { };

    private static readonly PackageCatalog _catalog = PackageCatalog.OpenForCurrentUser();
    private static readonly object _lock = new ();
    private readonly SemaphoreSlim _getInstalledPluginsLock = new (1, 1);
    private bool _disposedValue;

#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IPluginWrapper> _installedPlugins = new ();
    private static List<IPluginWrapper> _enabledPlugins = new ();
#pragma warning restore IDE0044 // Add readonly modifier

    public PluginService()
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
                foreach (var plugin in _installedPlugins)
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
        _installedPlugins.Clear();
        _enabledPlugins.Clear();
        OnPluginsChanged.Invoke(this, EventArgs.Empty);
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

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(bool includeDisabledPlugins = false)
    {
        await _getInstalledPluginsLock.WaitAsync();
        try
        {
            if (_installedPlugins.Count == 0)
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
                    var pluginWrapper = new PluginWrapper(name, extension.Package.Id.FullName, classId);

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
                    var isPluginDisabled = await localSettingsService.ReadSettingAsync<bool>(extension.Package.Id.FullName + "-ExtensionDisabled");

                    _installedPlugins.Add(pluginWrapper);
                    if (!isPluginDisabled)
                    {
                        _enabledPlugins.Add(pluginWrapper);
                    }
                }
            }

            return includeDisabledPlugins ? _installedPlugins : _enabledPlugins;
        }
        finally
        {
            _getInstalledPluginsLock.Release();
        }
    }

    public async Task<IEnumerable<IPluginWrapper>> StartAllPluginsAsync()
    {
        var installedPlugins = await GetInstalledPluginsAsync();
        foreach (var installedPlugin in installedPlugins)
        {
            if (!installedPlugin.IsRunning())
            {
                await installedPlugin.StartPluginAsync();
            }
        }

        return installedPlugins;
    }

    public async Task SignalStopPluginsAsync()
    {
        var installedPlugins = await GetInstalledPluginsAsync();
        foreach (var installedPlugin in installedPlugins)
        {
            if (installedPlugin.IsRunning())
            {
                installedPlugin.SignalDispose();
            }
        }
    }

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(ProviderType providerType, bool includeDisabledPlugins = false)
    {
        var installedPlugins = await GetInstalledPluginsAsync(includeDisabledPlugins);

        List<IPluginWrapper> filteredPlugins = new ();
        foreach (var installedPlugin in installedPlugins)
        {
            if (installedPlugin.HasProviderType(providerType))
            {
                filteredPlugins.Add(installedPlugin);
            }
        }

        return filteredPlugins;
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
                _getInstalledPluginsLock.Dispose();
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
}
