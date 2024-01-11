// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Services.WinGet;

namespace DevHome.SetupFlow.Services;

public interface IDesiredStateConfiguration
{
    /// <inheritdoc cref="IWinGetDeployment.IsConfigurationUnstubbedAsync" />"
    public Task<bool> IsUnstubbedAsync();

    /// <inheritdoc cref="IWinGetDeployment.UnstubConfigurationAsync" />"
    public Task<bool> UnstubAsync();

    /// <summary>
    /// Validates the provided configuration file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <param name="activityId">Activity ID.</param>
    public Task ValidateConfigurationAsync(string filePath, Guid activityId);

    /// <summary>
    /// Applies the provided configuration file.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <param name="activityId">Activity ID.</param>
    /// <returns>Result of applying the configuration.</returns>
    public Task<ConfigurationFileHelper.ApplicationResult> ApplyConfigurationAsync(string filePath, Guid activityId);
}
