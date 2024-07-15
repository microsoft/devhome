// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Services;

internal sealed class DSCDeployment : IDSCDeployment
{
    private readonly ILogger _logger;

    public DSCDeployment(ILogger<DSCDeployment> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsUnstubbedAsync()
    {
        try
        {
            return await Task.Run(() => new ConfigurationStaticFunctions().IsConfigurationAvailable);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred when checking if configuration is unstubbed");
            return false;
        }
    }

    public async Task<bool> UnstubAsync()
    {
        try
        {
            _logger.LogInformation("Starting to unstub configuration ...");
            await new ConfigurationStaticFunctions().EnsureConfigurationAvailableAsync();
            _logger.LogInformation("Configuration unstubbed successfully");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred when unstubbing configuration");
            return false;
        }
    }
}
