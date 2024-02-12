// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.DevSetupEngine;
using DevSetupEngineTypes = Microsoft.Windows.DevHome.DevSetupEngine;

namespace HyperVExtension.DevSetupAgent;

// Helper class to convert from the DevSetupEngine COM types to the .NET types and use them
// to serialize the results to JSON.
#pragma warning disable SA1402 // File may only contain a single type

public class ApplyConfigurationResult
{
    public ApplyConfigurationResult()
    {
    }

    public ApplyConfigurationResult(IApplyConfigurationResult applyConfigurationResult)
    {
        OpenConfigurationSetResult = new(applyConfigurationResult.OpenConfigurationSetResult);
        ApplyConfigurationSetResult = new(applyConfigurationResult.ApplyConfigurationSetResult);
    }

    public OpenConfigurationSetResult? OpenConfigurationSetResult { get; set; }

    public ApplyConfigurationSetResult? ApplyConfigurationSetResult { get; set; }
}

public class ConfigurationUnitResultInformation
{
    public ConfigurationUnitResultInformation()
    {
    }

    public ConfigurationUnitResultInformation(IConfigurationUnitResultInformation? configurationUnitResulInformation)
    {
        if (configurationUnitResulInformation != null)
        {
            ResultCode = configurationUnitResulInformation.ResultCode != null ? configurationUnitResulInformation.ResultCode.HResult : 0;
            Description = configurationUnitResulInformation.Description;
            Details = configurationUnitResulInformation.Details;
            ResultSource = configurationUnitResulInformation.ResultSource;
        }
    }

    // The error code of the failure.
    public int ResultCode { get; set; }

    // The short description of the failure.
    public string? Description { get; set; }

    // A more detailed error message appropriate for diagnosing the root cause of an error.
    public string? Details { get; set; }

    // The source of the result.
    public DevSetupEngineTypes.ConfigurationUnitResultSource ResultSource { get; set; }
}

public class OpenConfigurationSetResult
{
    public OpenConfigurationSetResult()
    {
    }

    public OpenConfigurationSetResult(IOpenConfigurationSetResult? openConfigurationSetResult)
    {
        if (openConfigurationSetResult != null)
        {
            ResultCode = openConfigurationSetResult.ResultCode != null ? openConfigurationSetResult.ResultCode.HResult : 0;
            Field = openConfigurationSetResult.Field;
            Value = openConfigurationSetResult.Value;
            Line = openConfigurationSetResult.Line;
            Column = openConfigurationSetResult.Column;
        }
    }

    // The result from opening the set.
    public int ResultCode { get; set; }

    // The field that is missing/invalid, if appropriate for the specific ResultCode.
    public string? Field { get; set; }

    // The value of the field, if appropriate for the specific ResultCode.
    public string? Value { get; set; }

    // The line number for the failure reason, if determined.
    public uint Line { get; set; }

    // The column number for the failure reason, if determined.
    public uint Column { get; set; }
}

public class ConfigurationUnit
{
    public ConfigurationUnit()
    {
    }

    public ConfigurationUnit(IConfigurationUnit? configurationUnit)
    {
        if (configurationUnit != null)
        {
            Type = configurationUnit.Type;
            Identifier = configurationUnit.Identifier;
            State = configurationUnit.State;
            IsGroup = configurationUnit.IsGroup;
            if (configurationUnit.Units != null)
            {
                var count = configurationUnit.Units.Count;
                if (count > 0)
                {
                    Units = new List<ConfigurationUnit>(count);
                    for (var i = 0; i < count; i++)
                    {
                        Units.Add(new ConfigurationUnit(configurationUnit.Units[i]));
                    }
                }
            }
        }
    }

    // The type of the unit being configured; not a name for this instance.
    public string? Type { get; set; }

    // The identifier name of this instance within the set.
    public string? Identifier { get; set; }

    // The current state of the configuration unit.
    public DevSetupEngineTypes.ConfigurationUnitState State { get; set; }

    // Determines if this configuration unit should be treated as a group.
    // A configuration unit group treats its `Settings` as the definition of child units.
    public bool IsGroup { get; set; }

    // The configuration units that are part of this unit (if IsGroup is true).
    public IList<ConfigurationUnit>? Units { get; set; }
}

public class ConfigurationSetChangeData
{
    public ConfigurationSetChangeData()
    {
    }

    public ConfigurationSetChangeData(IConfigurationSetChangeData? configurationSetChangeData)
    {
        if (configurationSetChangeData != null)
        {
            Change = configurationSetChangeData.Change;
            SetState = configurationSetChangeData.SetState;
            UnitState = configurationSetChangeData.UnitState;
            if (configurationSetChangeData.ResultInformation != null)
            {
                ResultInformation = new ConfigurationUnitResultInformation(configurationSetChangeData.ResultInformation);
            }

            if (configurationSetChangeData.Unit != null)
            {
                Unit = new ConfigurationUnit(configurationSetChangeData.Unit);
            }
        }
    }

    // The change event type that occurred.
    public DevSetupEngineTypes.ConfigurationSetChangeEventType Change { get; set; }

    // The state of the configuration set for this event (the ConfigurationSet can be used to get the current state, which may be different).
    public DevSetupEngineTypes.ConfigurationSetState SetState { get; set; }

    // The state of the configuration unit for this event (the ConfigurationUnit can be used to get the current state, which may be different).
    public DevSetupEngineTypes.ConfigurationUnitState UnitState { get; set; }

    // Contains information on the result of the attempt to apply the configuration unit.
    public ConfigurationUnitResultInformation? ResultInformation { get; set; }

    // The configuration unit whose state changed.
    public ConfigurationUnit? Unit { get; set; }
}

public class ApplyConfigurationUnitResult
{
    public ApplyConfigurationUnitResult()
    {
    }

    public ApplyConfigurationUnitResult(IApplyConfigurationUnitResult? applyConfigurationUnitResult)
    {
        if (applyConfigurationUnitResult != null)
        {
            if (applyConfigurationUnitResult.Unit != null)
            {
                Unit = new ConfigurationUnit(applyConfigurationUnitResult.Unit);
            }

            PreviouslyInDesiredState = applyConfigurationUnitResult.PreviouslyInDesiredState;
            RebootRequired = applyConfigurationUnitResult.RebootRequired;
            if (applyConfigurationUnitResult.ResultInformation != null)
            {
                ResultInformation = new ConfigurationUnitResultInformation(applyConfigurationUnitResult.ResultInformation);
            }
        }
    }

    // The configuration unit that was applied.
    public ConfigurationUnit? Unit { get; set; }

    // Will be true if the configuration unit was in the desired state (Test returns true) prior to the apply action.
    public bool PreviouslyInDesiredState { get; set; }

    // Indicates whether a reboot is required after the configuration unit was applied.
    public bool RebootRequired { get; set; }

    // The result of applying the configuration unit.
    public ConfigurationUnitResultInformation? ResultInformation { get; set; }
}

public class ApplyConfigurationSetResult
{
    public ApplyConfigurationSetResult()
    {
    }

    public ApplyConfigurationSetResult(IApplyConfigurationSetResult applyConfigurationSetResult)
    {
        if (applyConfigurationSetResult != null)
        {
            ResultCode = applyConfigurationSetResult.ResultCode != null ? applyConfigurationSetResult.ResultCode.HResult : 0;
            if (applyConfigurationSetResult.UnitResults != null)
            {
                var count = applyConfigurationSetResult.UnitResults.Count;
                if (count > 0)
                {
                    UnitResults = new List<ApplyConfigurationUnitResult>(count);
                    for (var i = 0; i < count; i++)
                    {
                        UnitResults.Add(new ApplyConfigurationUnitResult(applyConfigurationSetResult.UnitResults[i]));
                    }
                }
            }
        }
    }

    // Results for each configuration unit in the set.
    public List<ApplyConfigurationUnitResult>? UnitResults { get; set; }

    // The overall result from applying the configuration set.
    public int ResultCode { get; set; }
}

#pragma warning restore SA1402 // File may only contain a single type
