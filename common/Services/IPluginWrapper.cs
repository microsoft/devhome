// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.DevHome.SDK;
using WinRT;

namespace DevHome.Common.Services;
public interface IPluginWrapper
{
    /// <summary>
    /// Gets name of the plugin as mentioned in the manifest
    /// </summary>
    string Name
    {
        get;
    }

    /// <summary>
    /// Gets package fullname of the plugin
    /// </summary>
    string PackageFullName
    {
        get;
    }

    /// <summary>
    /// Gets class id (GUID) of the plugin class (which implements IPlugin) as mentioned in the manifest
    /// </summary>
    string Id
    {
        get;
    }

    /// <summary>
    /// Starts the plugin if not running and gets the provider from the underlying IPlugin object
    /// Can be null if not found
    /// </summary>
    /// <typeparam name="T">The type of provider</typeparam>
    /// <returns>Nullable instance of the provider</returns>
    Task<T?> GetProviderAsync<T>(bool invalidateCache = false)
        where T : class;

    bool HasProvider<T>();
}
