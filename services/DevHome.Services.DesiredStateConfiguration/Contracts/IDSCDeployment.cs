// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

internal interface IDSCDeployment
{
    /// <summary>
    /// Check if configuration is unstubbed
    /// </summary>
    /// <returns>True if configuration is unstubbed, false otherwise</returns>
    public Task<bool> IsUnstubbedAsync();

    /// <summary>
    /// Unstub configuration
    /// </summary>
    /// <returns>True if configuration was unstubbed, false otherwise</returns>
    public Task<bool> UnstubAsync();
}
