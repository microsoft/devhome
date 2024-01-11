// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Services.WinGet;
using Windows.Storage;

namespace DevHome.SetupFlow.Services;

internal class DesiredStateConfiguration : IDesiredStateConfiguration
{
    private readonly IWinGetDeployment _winGetDeployment;
    private readonly IWinGetRecovery _winGetRecovery;

    public DesiredStateConfiguration(IWinGetDeployment winGetDeployment, IWinGetRecovery winGetRecovery)
    {
        _winGetDeployment = winGetDeployment;
        _winGetRecovery = winGetRecovery;
    }

    /// <inheritdoc />
    public async Task<bool> IsStubbedAsync() => await _winGetDeployment.IsConfigurationStubbedAsync();

    /// <inheritdoc />
    public async Task<bool> UnstubAsync() => await _winGetDeployment.UnstubConfigurationAsync();

    /// <inheritdoc />
    public async Task ValidateConfigurationAsync(string filePath, Guid activityId)
    {
        // Try to open the configuration file to validate it.
        await OpenConfigurationSetAsync(filePath, activityId);
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
        var configFile = new ConfigurationFileHelper(activityId);
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        var content = File.ReadAllText(file.Path);
        await configFile.OpenConfigurationSetAsync(file.Path, content);
        return configFile;
    }
}
