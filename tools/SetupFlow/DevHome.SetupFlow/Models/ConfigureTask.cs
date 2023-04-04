// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.Services;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

internal class ConfigureTask : ISetupTask
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly StorageFile _file;
    private ConfigurationProcessor _processor;
    private ConfigurationSet _configSet;

    public bool RequiresAdmin => false;

    public bool RequiresReboot { get; private set; }

    public bool DependsOnDevDriveToBeInstalled => false;

    public ConfigureTask(ISetupFlowStringResource stringResource, StorageFile file)
    {
        _stringResource = stringResource;
        _file = file;
    }

    public async Task OpenConfigurationSetAsync()
    {
        try
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidDataException();
            var rootDirectory = Path.GetDirectoryName(assemblyDirectory) ?? throw new InvalidDataException();
            var modulesPath = Path.Combine(rootDirectory, "ExternalModules");

            var properties = new ConfigurationProcessorFactoryProperties();
            properties.AdditionalModulePaths = new List<string>() { modulesPath };
            var factory = new ConfigurationSetProcessorFactory(ConfigurationProcessorType.Hosted, properties);
            _processor = new ConfigurationProcessor(factory);
            var openResult = _processor.OpenConfigurationSet(await _file.OpenReadAsync());
            _configSet = openResult.Set;
            if (_configSet == null)
            {
                throw new OpenConfigurationSetException(openResult.ResultCode, openResult.Field);
            }
        }
        catch (Exception e)
        {
            _processor = null;
            _configSet = null;
            Log.Logger?.ReportError(nameof(ConfigureTask), $"Failed to open configuration set: {e.Message}");
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
                if (_processor == null || _configSet == null)
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
            catch (Exception e)
            {
                Log.Logger?.ReportError(nameof(ConfigureTask), $"Failed to apply configuration: {e.Message}");
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
