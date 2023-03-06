// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.Models;
using DevHome.SetupFlow.RepoConfig;
using DevHome.ViewModels;
using DevHome.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json;
using Windows.ApplicationModel.AppExtensions;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using WinRT;

namespace DevHome.Services;

public class PluginService : IPluginService
{
#pragma warning disable IDE0044 // Add readonly modifier
    private static List<IPluginWrapper> installedPlugins = new ();
#pragma warning restore IDE0044 // Add readonly modifier

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync()
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

    public async Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(ProviderType providerType)
    {
        var installedPlugins = await GetInstalledPluginsAsync();

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

public class Ole32
{
    // https://docs.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx
    public const int CLSCTXLOCALSERVER = 0x4;

    // https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cocreateinstance
    [DllImport(nameof(Ole32))]

#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern int CoCreateInstance(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IntPtr ppv);
#pragma warning restore CA1401 // P/Invokes should not be visible
}
