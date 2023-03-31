﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ConfigurationFile.Exceptions;
using DevHome.SetupFlow.ElevatedComponent;
using DevHome.Telemetry;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.ConfigurationFile.Models;

internal class ConfigureTask : ISetupTask
{
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly StorageFile _file;
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;

    public bool RequiresAdmin => false;

    public bool RequiresReboot { get; private set; }

    public bool DependsOnDevDriveToBeInstalled => false;

    public ConfigureTask(ILogger logger, ISetupFlowStringResource stringResource, StorageFile file)
    {
        _logger = logger;
        _stringResource = stringResource;
        _file = file;
    }

    public async Task OpenConfigurationSetAsync()
    {
        try
        {
            var factory = new ConfigurationSetProcessorFactory(ConfigurationProcessorType.Hosted, null);
            _processor = new ConfigurationProcessor(factory);
            var openResult = _processor.OpenConfigurationSet(await _file.OpenReadAsync());
            _configSet = openResult.Set;
            if (_configSet is null)
            {
                throw new OpenConfigurationSetException(openResult.ResultCode, openResult.Field);
            }
        }
        catch
        {
            _processor = null;
            _configSet = null;
            _logger.LogError(nameof(ConfigureTask), LogLevel.Local, "Failed to open configuration set");
            throw;
        }
    }

    TaskMessages ISetupTask.GetLoadingMessages()
    {
        return new ()
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplying),
            Error = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
            Finished = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccess),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccessReboot),
        };
    }

    public ActionCenterMessages GetErrorMessages()
    {
        return new ()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new ()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccessReboot),
        };
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                if (_processor is null || _configSet is null)
                {
                    return TaskFinishedState.Failure;
                }

                var result = await _processor.ApplySetAsync(_configSet, ApplyConfigurationSetFlags.None);
                if (result.ResultCode != null)
                {
                    throw result.ResultCode;
                }

                RequiresReboot = result.UnitResults.Any(result => result.RebootRequired);
                return TaskFinishedState.Success;
            }
            catch
            {
                _logger.LogError(nameof(ConfigureTask), LogLevel.Local, "Failed to apply configuration");
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    /// <inheritdoc/>
    /// <remarks><seealso cref="RequiresAdmin"/></remarks>
    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory)
    {
        // Noop
        return Task.FromResult(TaskFinishedState.Failure).AsAsyncOperation();
    }
}
