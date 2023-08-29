// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppExtensions;

namespace DevHome.Common.Services;
public interface IExtensionService
{
    Task<IEnumerable<IExtensionWrapper>> GetInstalledPluginsAsync(bool includeDisabledPlugins = false);

    Task<IEnumerable<IExtensionWrapper>> GetInstalledPluginsAsync(Microsoft.Windows.DevHome.SDK.ProviderType providerType, bool includeDisabledPlugins = false);

    Task<IEnumerable<IExtensionWrapper>> StartAllPluginsAsync();

    Task SignalStopPluginsAsync();

    Task<IEnumerable<AppExtension>> GetInstalledAppExtensionsAsync();

    public event EventHandler OnPluginsChanged;
}
