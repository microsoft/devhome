// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Contracts;

public interface IDSCOperations
{
    /// <summary>
    /// Validates the provided configuration file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <param name="activityId">Activity ID.</param>
    public Task ValidateConfigurationAsync(string filePath, Guid activityId);

    /// <summary>
    /// Gets the details of the provided configuration file.
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <param name="activityId">Activity ID.</param>
    /// <returns>List of configuration units</returns>
    public Task<IReadOnlyList<DSCConfigurationUnit>> GetConfigurationUnitDetailsAsync(DSCConfiguration configuration, Guid activityId);

    /// <summary>
    /// Applies the provided configuration file.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <param name="activityId">Activity ID.</param>
    /// <returns>Result of applying the configuration.</returns>
    public Task<DSCApplicationResult> ApplyConfigurationAsync(string filePath, Guid activityId);
}
