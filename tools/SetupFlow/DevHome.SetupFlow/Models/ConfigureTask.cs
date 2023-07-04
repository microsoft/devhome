// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Services;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

public class ConfigureTask : ISetupTask
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly StorageFile _file;
    private IConfigurationOperator _taskOperator;

    // Configuration files can run as either admin or as a regular user
    // depending on the user, make this settable.
    public bool RequiresAdmin { get; set; }

    public bool RequiresReboot { get; private set; }

    public bool DependsOnDevDriveToBeInstalled => false;

    public IList<IConfigurationUnitResult> UnitResults
    {
        get; private set;
    }

    public ConfigureTask(ISetupFlowStringResource stringResource, StorageFile file)
    {
        _stringResource = stringResource;
        _file = file;
    }

    public async Task OpenConfigurationSetAsync(ITaskOperatorFactory operatorFactory)
    {
        try
        {
            _taskOperator = operatorFactory.CreateConfigurationOperator();
            await _taskOperator.OpenConfigurationSetAsync(_file);
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

    /// <inheritdoc/>
    public IAsyncOperation<TaskFinishedState> Execute(ITaskOperatorFactory operatorFactory)
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Configuration, $"Starting application of configuration file {_file.Path}");
            return await ExecuteAsync(_taskOperator);
        }).AsAsyncOperation();
    }

    /// <inheritdoc/>
    /// <remarks><seealso cref="RequiresAdmin"/></remarks>
    public IAsyncOperation<TaskFinishedState> ExecuteAsAdmin(ITaskOperatorFactory elevatedOperatorFactory)
    {
        return Task.Run(async () =>
        {
            try
            {
                var taskOperator = elevatedOperatorFactory.CreateConfigurationOperator();

                // Re-open the configuration file in the elevated process
                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Opening configuration set in elevated process: {_file.Path}");
                await taskOperator.OpenConfigurationSetAsync(_file);

                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Starting elevated application of configuration file");
                return await ExecuteAsync(taskOperator);
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to execute {nameof(ConfigureTask)} in elevated process", e);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    private async Task<TaskFinishedState> ExecuteAsync(IConfigurationOperator taskOperator)
    {
        var result = await taskOperator.ApplyConfigurationAsync();
        RequiresReboot = result.RebootRequired;
        UnitResults = new List<IConfigurationUnitResult>();

        // Cannot use foreach or LINQ for out-of-process IVector
        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
        for (var i = 0; i < result.UnitResults.Count; ++i)
        {
            UnitResults.Add(result.UnitResults[i]);
        }

        return result.Succeeded ? TaskFinishedState.Success : TaskFinishedState.Failure;
    }
}
