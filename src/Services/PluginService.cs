// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Models;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel.AppExtensions;
using Windows.Foundation.Collections;

namespace DevHome.Services;

public class PluginService : IPluginService
{
#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IPluginWrapper> installedPlugins = new ();
#pragma warning restore IDE0044 // Add readonly modifier

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(bool includeDisabledPlugins = false)
    {
        if (installedPlugins.Count == 0)
        {
            var extensions = await AppExtensionCatalog.Open("com.microsoft.devhome").FindAllAsync();
            foreach (var extension in extensions)
            {
                var properties = await extension.GetExtensionPropertiesAsync();

                var devHomeProvider = GetSubPropertySet(properties, "DevHomeProvider");
                if (devHomeProvider is null)
                {
                    continue;
                }

                var activation = GetSubPropertySet(devHomeProvider, "Activation");
                if (activation is null)
                {
                    continue;
                }

                var comActivation = GetSubPropertySet(activation, "CreateInstance");
                if (comActivation is null)
                {
                    continue;
                }

                var classId = GetProperty(comActivation, "@ClassId");
                if (classId is null)
                {
                    continue;
                }

                if (!includeDisabledPlugins)
                {
                    var isDisabled = Task.Run(() =>
                    {
                        var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                        return localSettingsService.ReadSettingAsync<bool>(classId + "-ExtensionDisabled");
                    }).Result;
                    if (isDisabled)
                    {
                        continue;
                    }
                }

                var name = extension.DisplayName;
                var pluginWrapper = new PluginWrapper(name, classId);

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
                        }
                    }
                }

                installedPlugins.Add(pluginWrapper);
            }
        }

        return installedPlugins;
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

    private IPropertySet? GetSubPropertySet(IPropertySet propSet, string name)
    {
        return propSet[name] as IPropertySet;
    }

    private string? GetProperty(IPropertySet propSet, string name)
    {
        return propSet[name] as string;
    }
}
