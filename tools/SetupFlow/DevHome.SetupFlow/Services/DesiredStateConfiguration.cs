// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services.WinGet;
using Windows.Storage;

namespace DevHome.SetupFlow.Services;

internal sealed class DesiredStateConfiguration : IDesiredStateConfiguration
{
    private readonly IWinGetDeployment _winGetDeployment;

    public DesiredStateConfiguration(IWinGetDeployment winGetDeployment)
    {
        _winGetDeployment = winGetDeployment;
    }

    /// <inheritdoc />
    public async Task<bool> IsUnstubbedAsync() => await _winGetDeployment.IsConfigurationUnstubbedAsync();

    /// <inheritdoc />
    public async Task<bool> UnstubAsync() => await _winGetDeployment.UnstubConfigurationAsync();

    /// <inheritdoc />
    public async Task ValidateConfigurationAsync(string filePath, Guid activityId)
    {
        // Try to open the configuration file to validate it.
        await OpenConfigurationSetAsync(filePath, activityId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DSCConfigurationUnit>> GetConfigurationUnitDetailsAsync(Configuration configuration, Guid activityId)
    {
        var configFile = await OpenConfigurationSetAsync(configuration.Path, configuration.Content, activityId);
        await configFile.ResolveConfigurationUnitDetails();
        var configUnitsOutOfProc = configFile.Units;
        var configUnitsInProc = configUnitsOutOfProc.Select(unit => new DSCConfigurationUnit(unit));
        return configUnitsInProc.ToList();
    }

    /// <inheritdoc />
    public async Task<ConfigurationFileHelper.ApplicationResult> ApplyConfigurationAsync(string filePath, Guid activityId)
    {
        // Apply the configuration file after opening it.
        var openConfigSet = await OpenConfigurationSetAsync(filePath, activityId);
        return await openConfigSet.ApplyConfigurationAsync();
    }

    /// <summary>
    /// Open the configuration set
    /// </summary>
    /// <param name="filePath">Configuration file path</param>
    /// <param name="activityId">Activity ID</param>
    /// <returns>Configuration file helper</returns>
    private async Task<ConfigurationFileHelper> OpenConfigurationSetAsync(string filePath, Guid activityId)
    {
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var content = File.ReadAllText(file.Path);
        return await OpenConfigurationSetAsync(file.Path, content, activityId);
    }

    private async Task<ConfigurationFileHelper> OpenConfigurationSetAsync(string filePath, string content, Guid activityId)
    {
        var configFile = new ConfigurationFileHelper(activityId);
        await configFile.OpenConfigurationSetAsync(filePath, content);
        return configFile;
    }
}
