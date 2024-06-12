// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

internal interface IDSCOperations
{
    /// <summary>
    /// Apply DSC configuration from a file
    /// </summary>
    /// <param name="file">File containing the DSC configuration</param>
    /// <param name="activityId">Activity ID for telemetry</param>
    /// <returns>Result of applying the configuration</returns>
    public Task<IDSCApplicationResult> ApplyConfigurationAsync(IDSCFile file, Guid activityId);

    /// <summary>
    /// Get details of configuration units in a file
    /// </summary>
    /// <param name="file">File containing the DSC configuration</param>
    /// <returns>Details of configuration units</returns>
    public Task<IDSCSet> GetConfigurationUnitDetailsAsync(IDSCFile file);

    /// <summary>
    /// Validate the configuration in a file
    /// </summary>
    /// <param name="file">File containing the DSC configuration</param>
    public Task ValidateConfigurationAsync(IDSCFile file);
}
