// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Helpers;
using DevHome.SetupFlow.Services;
using Microsoft.Management.Configuration;
using Microsoft.Management.Configuration.Processor;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

public class ConfigureTask : ISetupTask
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly StorageFile _file;
    private ConfigurationFileHelper _configurationFileHelper;

    // We can run configuration files as admin or as regular user
    // depending on the user, so we make this settable.
    public bool RequiresAdmin { get; set; }

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
            _configurationFileHelper = new ConfigurationFileHelper(_file);
            await _configurationFileHelper.OpenConfigurationSetAsync();
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to open configuration set: {e.Message}");
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
                await _configurationFileHelper.ApplyConfigurationAsync();
                RequiresReboot = _configurationFileHelper.ResultRequiresReboot;
                if (_configurationFileHelper.ApplicationSucceeded)
                {
                    return TaskFinishedState.Success;
                }
                else
                {
                    throw _configurationFileHelper.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration: {e.Message}");
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    /// <inheritdoc/>
    /// <remarks><seealso cref="RequiresAdmin"/></remarks>
    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory)
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Configuration, $"Starting elevated application of configuration file {_file.Path}");
            var elevatedTask = elevatedComponentFactory.CreateElevatedConfigurationTask();
            var elevatedResult = await elevatedTask.ApplyConfiguration(_file);
            RequiresReboot = elevatedResult.RebootRequired;
            return elevatedResult.TaskSucceeded ? TaskFinishedState.Success : TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }
}
