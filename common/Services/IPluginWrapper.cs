// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Services;
public interface IExtensionWrapper
{
    /// <summary>
    /// Gets name of the extension as mentioned in the manifest
    /// </summary>
    string Name
    {
        get;
    }

    /// <summary>
    /// Gets PackageFullName of the extension
    /// </summary>
    string PackageFullName
    {
        get;
    }

    /// <summary>
    /// Gets class id (GUID) of the extension class (which implements IExtension) as mentioned in the manifest
    /// </summary>
    string ExtensionClassId
    {
        get;
    }

    /// <summary>
    /// Checks whether we have a reference to the extension process and we are able to call methods on the interface.
    /// </summary>
    /// <returns>Whether we have a reference to the extension process and we are able to call methods on the interface.</returns>
    bool IsRunning();

    /// <summary>
    /// Starts the extension if not running
    /// </summary>
    /// <returns>An awaitable task</returns>
    Task StartExtensionAsync();

    /// <summary>
    /// Signals the extension to dispose itself and removes the reference to the extension com object
    /// </summary>
    void SignalDispose();

    /// <summary>
    /// Gets the underlying instance of IExtension
    /// </summary>
    /// <returns>Instance of IExtension</returns>
    IExtension? GetExtensionObject();

    /// <summary>
    /// Tells the wrapper that the extension implements the given provider
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
    /// Starts the extension if not running and gets the provider from the underlying IExtension object
    /// Can be null if not found
    /// </summary>
    /// <typeparam name="T">The type of provider</typeparam>
    /// <returns>Nullable instance of the provider</returns>
    Task<T?> GetProviderAsync<T>()
        where T : class;
}
