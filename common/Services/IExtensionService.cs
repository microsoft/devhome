// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Models.ExtensionJsonData;
using Windows.Foundation;

namespace DevHome.Common.Services;

public interface IExtensionService
{
    Task<IEnumerable<IExtensionWrapper>> GetInstalledExtensionsAsync(bool includeDisabledExtensions = false);

    Task<IEnumerable<string>> GetInstalledDevHomeWidgetPackageFamilyNamesAsync(bool includeDisabledExtensions = false);

    Task<IEnumerable<IExtensionWrapper>> GetInstalledExtensionsAsync(Microsoft.Windows.DevHome.SDK.ProviderType providerType, bool includeDisabledExtensions = false);

    IExtensionWrapper? GetInstalledExtension(string extensionUniqueId);

    Task SignalStopExtensionsAsync();

    public event EventHandler OnExtensionsChanged;

    public event TypedEventHandler<IExtensionService, IExtensionWrapper> ExtensionToggled;

    public void EnableExtension(string extensionUniqueId);

    public void DisableExtension(string extensionUniqueId);

    /// <summary>
    /// Gets a boolean indicating whether the extension was disabled due to the corresponding Windows optional feature
    /// being absent from the machine or in an unknown state.
    /// </summary>
    /// <param name="extension">The out of proc extension object</param>
    /// <returns>True only if the extension was disabled. False otherwise.</returns>
    public Task<bool> DisableExtensionIfWindowsFeatureNotAvailable(IExtensionWrapper extension);

    /// <summary>
    /// Gets known extension information from internal extension json file.
    /// </summary>
    /// <returns>An object that holds a list of extension information based on the internal json file.</returns>
    public Task<DevHomeExtensionContentData?> GetExtensionJsonDataAsync();
}
