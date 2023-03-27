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
    /// Gets class id (GUID) of the plugin class (which implements IPlugin) as mentioned in the manifest
    /// </summary>
    string PluginClassId
    {
        get;
    }

    /// <summary>
    /// Checks whether we have a reference to the plugin process and we are able to call methods on the interface.
    /// </summary>
    /// <returns>whether we have a reference to the plugin process and we are able to call methods on the interface</returns>
    bool IsRunning();

    /// <summary>
    /// Starts the plugin if not running
    /// </summary>
    /// <returns>An awaitable task</returns>
    Task StartPluginAsync();

    /// <summary>
    /// Signals the plugin to dispose itself and removes the reference to the plugin com object
    /// </summary>
    void SignalDispose();

    /// <summary>
    /// Gets the underlying instance of IPlugin
    /// </summary>
    /// <returns>Instance of IPlugin</returns>
    IPlugin? GetPluginObject();

    /// <summary>
    /// Tells the wrapper that the plugin implements the given provider
    /// </summary>
    /// <param name="providerType">The type of provider to be added</param>
    void AddProviderType(ProviderType providerType);

    /// <summary>
    /// Checks whether the given provider was added through `AddProviderType` method
    /// </summary>
    /// <param name="providerType">The type of the provider to be checked for</param>
    /// <returns>Whether the given provider was added through `AddProviderType` method</returns>
    bool HasProviderType(ProviderType providerType);

    /// <summary>
    /// Starts the plugin if not running and gets the provider from the underlying IPlugin object
    /// Can be null if not found
    /// </summary>
    /// <typeparam name="T">The type of provider</typeparam>
    /// <returns>Nullable instance of the provider</returns>
    Task<T?> GetProviderAsync<T>()
        where T : class;
}
