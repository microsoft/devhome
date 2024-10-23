// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.SetupFlow.Common.Contracts;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;

namespace DevHome.SetupFlow.Models;

public class ConfigureTask : ISetupTask
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ConfigureTask));
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IDSC _dsc;
    private readonly IDSCFile _file;
    private readonly Guid _activityId;

    public event ISetupTask.ChangeMessageHandler AddMessage;

#pragma warning disable 67
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;
#pragma warning restore 67

    // Configuration files can run as either admin or as a regular user
    // depending on the user, make this settable.
    public bool RequiresAdmin { get; set; }

    public bool RequiresReboot { get; private set; }

    /// <summary>
    /// Gets target device name. Inherited via ISetupTask but unused.
    /// </summary>
    public string TargetName => string.Empty;

    public bool DependsOnDevDriveToBeInstalled => false;

    public IList<ConfigurationUnitResult> UnitResults
    {
        get; private set;
    }

    public ISummaryInformationViewModel SummaryScreenInformation { get; }

    public ConfigureTask(
        ISetupFlowStringResource stringResource,
        IDSC dsc,
        IDSCFile file,
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

    IAsyncOperationWithProgress<TaskFinishedState, TaskProgress> ISetupTask.Execute()
    {
        return AsyncInfo.Run<TaskFinishedState, TaskProgress>(async (_, _) =>
        {
            try
            {
                AddMessage(_stringResource.GetLocalized(StringResourceKey.ApplyingConfigurationMessage), MessageSeverityKind.Info);
                var result = await _dsc.ApplyConfigurationAsync(_file, _activityId);
                RequiresReboot = result.RequiresReboot;
                UnitResults = result.UnitResults.Select(unitResult => new ConfigurationUnitResult(unitResult)).ToList();
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
                _log.Error(e, $"Failed to apply configuration.");
                return TaskFinishedState.Failure;
            }
        });
    }

    /// <inheritdoc/>
    /// <remarks><seealso cref="RequiresAdmin"/></remarks>
    IAsyncOperationWithProgress<TaskFinishedState, TaskProgress> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation)
    {
        return AsyncInfo.Run<TaskFinishedState, TaskProgress>(async (_, _) =>
        {
            _log.Information($"Starting elevated application of configuration file {_file.Path}");
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
        });
    }

    /// <summary>
    /// Get the arguments for this task
    /// </summary>
    /// <returns>Arguments for this task</returns>
    public ConfigureTaskArguments GetArguments()
    {
        return new()
        {
            FilePath = _file.Path,
            Content = _file.Content,
        };
    }
}
