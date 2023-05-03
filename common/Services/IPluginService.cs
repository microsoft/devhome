// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.AppExtensions;

namespace DevHome.Common.Services;
public interface IPluginService
{
    Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(bool includeDisabledPlugins = false);

    Task SignalStopPluginsAsync();

    Task<IEnumerable<AppExtension>> GetInstalledAppExtensionsAsync();

    Task<Models.ExtensionQueryResult<TResult>> RunQueryAsync<TResult>(Func<Task<TResult>> query, int timeoutMs = 200);

    Models.ExtensionQueryResult<TResult> RunQueryAsync<TResult>(Func<TResult> query);

    public event EventHandler OnPluginsChanged;
}
