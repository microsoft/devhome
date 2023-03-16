// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Services;
public interface IPluginService
{
    Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync();

    Task<IEnumerable<IPluginWrapper>> GetInstalledPluginsAsync(Microsoft.Windows.DevHome.SDK.ProviderType providerType);
}
