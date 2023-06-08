// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Helpers;
using DevHome.Models;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;

namespace DevHome.Services;

public class PluginService : IPluginService
{
    public event EventHandler OnPluginsChanged = (_, _) => { };

    private static readonly PackageCatalog _catalog = PackageCatalog.OpenForCurrentUser();
    private static readonly object _lock = new ();

#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IPluginWrapper> _installedPlugins = new ();
    private static List<IPluginWrapper> _enabledPlugins = new ();
#pragma warning restore IDE0044 // Add readonly modifier

    private readonly IReadOnlyDictionary<string, Type> _providerTypeMap = new Dictionary<string, Type>()
    {
        ["DeveloperIdProvider"] = typeof(IDeveloperIdProvider),
        ["RepositoryProvider"] = typeof(IRepositoryProvider),
    };

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
                return await ParsePlugin(extension) is not null;
            }
        }

        return false;
    }

    private async Task<IPluginWrapper?> ParsePlugin(AppExtension extension)
    {
        var properties = await extension.GetExtensionPropertiesAsync();
        var id = extension.Id;
        var name = extension.DisplayName;
        var description = extension.Description;
        var publicFolder = await extension.GetPublicFolderAsync();
        var packageFullName = extension.Package.Id.FullName;
        var providers = new Dictionary<Type, string>();

        foreach (var property in properties)
        {
            if (_providerTypeMap.TryGetValue(property.Key, out var providerType))
            {
                var value = property.Value;
                if (value is null)
                {
                    Log.Logger?.ReportDebug($"{name}: Property value for {property.Key} cannot be null.");
                    continue;
                }

                if (value is not IPropertySet)
                {
                    Log.Logger?.ReportDebug($"{name}: Invalid property value for {property.Key}");
                    continue;
                }

                var activation = GetSubPropertySet((property.Value as IPropertySet)!, "Activation");
                if (activation is null)
                {
                    Log.Logger?.ReportDebug($"{name}: {property.Key} must have an Activation element");
                    continue;
                }

                var comActivation = GetSubPropertySet(activation, "CreateInstance");
                if (comActivation is null)
                {
                    Log.Logger?.ReportDebug($"{name}: Activation element must have a CreateInstance element");
                    continue;
                }

                var classId = GetProperty(comActivation, "@ClassId");
                if (classId is null)
                {
                    Log.Logger?.ReportDebug($"{name}: CreateInstance must have a ClassId attribute");
                    continue;
                }

                providers.Add(providerType, classId);
            }
            else
            {
                Log.Logger?.ReportDebug($"{name}: Unsupported property {property.Key}");
            }
        }

        if (!providers.Any())
        {
            Log.Logger?.ReportWarn($"Invalid Extension {id}, {name}. It does not support any valid providers.");
            return null;
        }

        return new PluginWrapper(id, name, description, publicFolder, packageFullName, providers);
    }

    public async Task<IEnumerable<AppExtension>> GetInstalledAppExtensionsAsync()
    {
        return await AppExtensionCatalog.Open("com.microsoft.devhome").FindAllAsync();
    }

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(bool includeDisabledPlugins = false)
    {
        if (_installedPlugins.Count == 0)
        {
            var extensions = await GetInstalledAppExtensionsAsync();
            foreach (var extension in extensions)
            {
                var pluginWrapper = await ParsePlugin(extension);
                if (pluginWrapper is null)
                {
                    continue;
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

    public async Task<ExtensionQueryResult<TResult>> RunQueryAsync<TResult>(Func<Task<TResult>> query, int timeoutMs = 200)
    {
        try
        {
            var result = await query.Invoke().WithTimeout(timeoutMs);
            return new ExtensionQueryResult<TResult>(true, result, null);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError($"Exception occured while calling an extension api", e);
            return new ExtensionQueryResult<TResult>(false, default, e);
        }
    }

    public ExtensionQueryResult<TResult> RunQueryAsync<TResult>(Func<TResult> query)
    {
        try
        {
            var result = query.Invoke();
            return new ExtensionQueryResult<TResult>(true, result, null);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError($"Exception occured while calling an extension api", e);
            return new ExtensionQueryResult<TResult>(false, default, e);
        }
    }

    private IPropertySet? GetSubPropertySet(IPropertySet propSet, string name)
    {
        if (propSet.TryGetValue(name, out var value))
        {
            return value is IPropertySet ? value as IPropertySet : null;
        }

        return null;
    }

    private string? GetProperty(IPropertySet propSet, string name)
    {
        if (propSet.TryGetValue(name, out var value))
        {
            return value is string ? value as string : null;
        }

        return null;
    }

    public async Task SignalStopPluginsAsync()
    {
        var plugins = await GetInstalledPluginsAsync();
        foreach (var plugin in plugins)
        {
            plugin.Dispose();
        }
    }
}
