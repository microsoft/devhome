// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Views;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

public class ConfigureTask : ISetupTask
{
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IDesiredStateConfiguration _dsc;
    private readonly StorageFile _file;
    private readonly Guid _activityId;

    public event ISetupTask.ChangeMessageHandler AddMessage;

#pragma warning disable 67
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    // Configuration files can run as either admin or as a regular user
    // depending on the user, make this settable.
    public bool RequiresAdmin { get; set; }

    public bool RequiresReboot { get; private set; }

    public bool DependsOnDevDriveToBeInstalled => false;

    public IList<ConfigurationUnitResult> UnitResults
    {
        get; private set;
    }

    public ConfigureTask(
        ISetupFlowStringResource stringResource,
        IDesiredStateConfiguration dsc,
        StorageFile file,
        Guid activityId)
    {
        _stringResource = stringResource;
        _dsc = dsc;
        _file = file;
        _activityId = activityId;
    }

    TaskMessages ISetupTask.GetLoadingMessages()
    {
        return new()
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplying),
            Error = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
            Finished = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccess),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccessReboot),
        };
    }

    public ActionCenterMessages GetErrorMessages()
    {
        return new()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new()
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
                AddMessage(_stringResource.GetLocalized(StringResourceKey.ApplyingConfigurationMessage), MessageSeverityKind.Info);
                var result = await _dsc.ApplyConfigurationAsync(_file.Path, _activityId);
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
    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation)
    {
        return Task.Run(async () =>
        {
            Log.Logger?.ReportInfo(Log.Component.Configuration, $"Starting elevated application of configuration file {_file.Path}");
            var elevatedResult = await elevatedComponentOperation.ApplyConfigurationAsync(_activityId);
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

    /// <summary>
    /// Get the arguments for this task
    /// </summary>
    /// <returns>Arguments for this task</returns>
    public ConfigureTaskArguments GetArguments()
    {
        var fileData = GetFileData();
        return new ConfigureTaskArguments
        {
            FilePath = fileData.FilePath,
            Content = fileData.Content,
        };
    }

    private (string FilePath, string Content) GetFileData()
    {
        var content = File.ReadAllText(_file.Path);
        return (_file.Path, content);
    }
}
