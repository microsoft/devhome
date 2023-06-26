// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Configuration;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;
using Windows.Storage;
using WinRT;

namespace DevHome.SetupFlow.Models;

public class ConfigureTask : ISetupTask
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly StorageFile _file;
    private ConfigurationFileHelper _configurationFileHelper;

    // Configuration files can run as either admin or as a regular user
    // depending on the user, make this settable.
    public bool RequiresAdmin { get; set; }

    public bool RequiresReboot { get; private set; }

    public bool DependsOnDevDriveToBeInstalled => false;

    public IList<ConfigurationUnitResult> UnitResults
    {
        get; private set;
    }

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
            Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to open configuration set.", e);
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
                var result = await _configurationFileHelper.ApplyConfigurationAsync();
                RequiresReboot = result.RequiresReboot;
                UnitResults = result.Result.UnitResults.Select(unitResult => new ConfigurationUnitResult(unitResult)).ToList();
                if (result.Succeeded)
                {
                    return TaskFinishedState.Success;
                }
                else
                {
                    throw result.ResultException;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration.", e);
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
            UnitResults = new List<ConfigurationUnitResult>();

            // Cannot use foreach or LINQ for out-of-process IVector
            // Bug: https://github.com/microsoft/CsWinRT/issues/1205
            for (var i = 0; i < elevatedResult.UnitResults.Count; ++i)
            {
                UnitResults.Add(new ConfigurationUnitResult(elevatedResult.UnitResults[i]));
            }

            return elevatedResult.TaskSucceeded ? TaskFinishedState.Success : TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }
}
