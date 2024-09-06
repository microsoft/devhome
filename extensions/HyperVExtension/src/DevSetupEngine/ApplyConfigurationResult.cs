// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//// Implementation of the interfaces used to send progress information and
//// results of applying a configuration set via WinGet.
//// These interfaces and classes are similar to WinGet's interfaces, but simplified for our use case.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.DevSetupEngine;
using Windows.Foundation.Collections;
using DevSetupEngineTypes = Microsoft.Windows.DevHome.DevSetupEngine;

namespace HyperVExtension.DevSetupEngine.ConfigurationResultTypes;

#pragma warning disable SA1402 // File may only contain a single type

[ComVisible(true)]
[Guid("36DB1AC2-B5D0-462E-82E3-7AB474C6DEFD")]
[ComDefaultInterface(typeof(IApplyConfigurationResult))]
public partial class ApplyConfigurationResult : IApplyConfigurationResult
{
    public ApplyConfigurationResult(Exception? resultCode, string resultDescription, IOpenConfigurationSetResult? openConfigurationSetResult, IApplyConfigurationSetResult? applyConfigurationSetResult)
    {
        ResultCode = resultCode;
        ResultDescription = resultDescription;
        OpenConfigurationSetResult = openConfigurationSetResult;
        ApplyConfigurationSetResult = applyConfigurationSetResult;
    }

    public Exception? ResultCode { get; }

    public string ResultDescription { get; }

    public IOpenConfigurationSetResult? OpenConfigurationSetResult { get; }

    public IApplyConfigurationSetResult? ApplyConfigurationSetResult { get; }
}

[ComVisible(true)]
[Guid("8eecacf2-d864-46c5-aeb1-e66e96a8f654")]
[ComDefaultInterface(typeof(IConfigurationUnitResultInformation))]
public partial class ConfigurationUnitResultInformation : IConfigurationUnitResultInformation
{
    public ConfigurationUnitResultInformation(Exception? resultCode, string description, string details, DevSetupEngineTypes.ConfigurationUnitResultSource resultSource)
    {
        ResultCode = resultCode;
        Description = description;
        Details = details;
        ResultSource = resultSource;
    }

    // The error code of the failure.
    public Exception? ResultCode { get; }

    // The short description of the failure.
    public string Description { get; }

    // A more detailed error message appropriate for diagnosing the root cause of an error.
    public string Details { get; }

    // The source of the result.
    public DevSetupEngineTypes.ConfigurationUnitResultSource ResultSource { get; }
}

[ComVisible(true)]
[Guid("8c4db755-62e6-4c5e-b42a-cd8d27e3b675")]
[ComDefaultInterface(typeof(IOpenConfigurationSetResult))]
public partial class OpenConfigurationSetResult : IOpenConfigurationSetResult
{
    public OpenConfigurationSetResult(Exception? resultCode, string field, string fieldValue, uint line, uint column)
    {
        ResultCode = resultCode;
        Field = field;
        Value = fieldValue;
        Line = line;
        Column = column;
    }

    // The result from opening the set.
    public Exception? ResultCode { get; }

    // The field that is missing/invalid, if appropriate for the specific ResultCode.
    public string Field { get; }

    // The value of the field, if appropriate for the specific ResultCode.
    public string Value { get; }

    // The line number for the failure reason, if determined.
    public uint Line { get; }

    // The column number for the failure reason, if determined.
    public uint Column { get; }
}

[ComVisible(true)]
[Guid("6a2a0231-ea0e-4d71-97a5-922f340af798")]
[ComDefaultInterface(typeof(IConfigurationUnit))]
public partial class ConfigurationUnit : IConfigurationUnit
{
    public ConfigurationUnit(string type, string identifier, DevSetupEngineTypes.ConfigurationUnitState state, bool isGroup, IList<IConfigurationUnit>? units, ValueSet? settings, ConfigurationUnitIntent intent)
    {
        Type = type;
        Identifier = identifier;
        State = state;
        IsGroup = isGroup;
        Units = units;
        Settings = settings;
        Intent = intent;
    }

    // The type of the unit being configured; not a name for this instance.
    public string Type { get; }

    // The identifier name of this instance within the set.
    public string Identifier { get; }

    // The current state of the configuration unit.
    public DevSetupEngineTypes.ConfigurationUnitState State { get; }

    // Determines if this configuration unit should be treated as a group.
    // A configuration unit group treats its `Settings` as the definition of child units.
    public bool IsGroup { get; }

    // The configuration units that are part of this unit (if IsGroup is true).
    public IList<IConfigurationUnit>? Units { get; }

    public DevSetupEngineTypes.ConfigurationUnitIntent Intent { get; }

    public ValueSet? Settings { get; }
}

[ComVisible(true)]
[Guid("a4915b80-fbb1-4c8f-bf86-35fd071a8a50")]
[ComDefaultInterface(typeof(IConfigurationSetChangeData))]
public partial class ConfigurationSetChangeData : IConfigurationSetChangeData
{
    public ConfigurationSetChangeData(DevSetupEngineTypes.ConfigurationSetChangeEventType change, DevSetupEngineTypes.ConfigurationSetState setState, DevSetupEngineTypes.ConfigurationUnitState unitState, IConfigurationUnitResultInformation resultInformation, IConfigurationUnit unit)
    {
        Change = change;
        SetState = setState;
        UnitState = unitState;
        ResultInformation = resultInformation;
        Unit = unit;
    }

    // The change event type that occurred.
    public DevSetupEngineTypes.ConfigurationSetChangeEventType Change { get; }

    // The state of the configuration set for this event (the ConfigurationSet can be used to get the current state, which may be different).
    public DevSetupEngineTypes.ConfigurationSetState SetState { get; }

    // The state of the configuration unit for this event (the ConfigurationUnit can be used to get the current state, which may be different).
    public DevSetupEngineTypes.ConfigurationUnitState UnitState { get; }

    // Contains information on the result of the attempt to apply the configuration unit.
    public IConfigurationUnitResultInformation ResultInformation { get; }

    // The configuration unit whose state changed.
    public IConfigurationUnit Unit { get; }
}

[ComVisible(true)]
[Guid("6bf246e5-d4a4-4593-9253-778027f18197")]
[ComDefaultInterface(typeof(IApplyConfigurationUnitResult))]
public partial class ApplyConfigurationUnitResult : IApplyConfigurationUnitResult
{
    public ApplyConfigurationUnitResult(IConfigurationUnit unit, ConfigurationUnitState state, bool previouslyInDesiredState, bool rebootRequired, IConfigurationUnitResultInformation resultInformation)
    {
        Unit = unit;
        PreviouslyInDesiredState = previouslyInDesiredState;
        RebootRequired = rebootRequired;
        ResultInformation = resultInformation;
        State = state;
    }

    // The configuration unit that was applied.
    public IConfigurationUnit Unit { get; }

    // Will be true if the configuration unit was in the desired state (Test returns true) prior to the apply action.
    public bool PreviouslyInDesiredState { get; }

    // Indicates whether a reboot is required after the configuration unit was applied.
    public bool RebootRequired { get; }

    // The result of applying the configuration unit.
    public IConfigurationUnitResultInformation ResultInformation { get; }

    public ConfigurationUnitState State { get; }
}

[ComVisible(true)]
[Guid("0ca281ff-537c-4919-a268-1bd67d83dbfb")]
[ComDefaultInterface(typeof(IApplyConfigurationSetResult))]
public partial class ApplyConfigurationSetResult : IApplyConfigurationSetResult
{
    public ApplyConfigurationSetResult(Exception? resultCode, IReadOnlyList<IApplyConfigurationUnitResult>? unitResults)
    {
        ResultCode = resultCode;
        UnitResults = unitResults;
    }

    // Results for each configuration unit in the set.
    public IReadOnlyList<IApplyConfigurationUnitResult>? UnitResults { get; }

    // The overall result from applying the configuration set.
    public Exception? ResultCode { get; }
}

#pragma warning restore SA1402 // File may only contain a single type
