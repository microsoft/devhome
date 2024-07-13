// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using Windows.Foundation.Collections;

namespace HyperVExtension.HostGuestCommunication;

// Helper class to convert from the DevSetupEngine COM types to the .NET types and use them
// to serialize the results to JSON.
#pragma warning disable SA1402 // File may only contain a single type

public enum ConfigurationSetChangeEventType : int
{
    Unknown = 0,
    SetStateChanged = 0x1,
    UnitStateChanged = 0x2,
}

public enum ConfigurationSetState : int
{
    Unknown = 0,
    Pending = 0x1,
    InProgress = 0x2,
    Completed = 0x3,
}

public enum ConfigurationUnitResultSource : int
{
    None = 0,
    Internal = 0x1,
    ConfigurationSet = 0x2,
    UnitProcessing = 0x3,
    SystemState = 0x4,
    Precondition = 0x5,
}

public enum ConfigurationUnitState : int
{
    Unknown = 0,
    Pending = 0x1,
    InProgress = 0x2,
    Completed = 0x3,
    Skipped = 0x4,
}

public enum ConfigurationUnitIntent : int
{
    Assert,
    Inform,
    Apply,
    Unknown,
}

public class ApplyConfigurationResult
{
    public ApplyConfigurationResult()
    {
    }

    public ApplyConfigurationResult(int resultCode, string? resultDescription = null)
    {
        ResultCode = resultCode;
        ResultDescription = resultDescription ?? string.Empty;
    }

    public int ResultCode { get; set; }

    public string ResultDescription { get; set; } = string.Empty;

    public OpenConfigurationSetResult? OpenConfigurationSetResult { get; set; }

    public ApplyConfigurationSetResult? ApplyConfigurationSetResult { get; set; }
}

public class ConfigurationUnitResultInformation
{
    public ConfigurationUnitResultInformation()
    {
    }

    // The error code of the failure.
    public int ResultCode { get; set; }

    // The short description of the failure.
    public string? Description { get; set; }

    // A more detailed error message appropriate for diagnosing the root cause of an error.
    public string? Details { get; set; }

    // The source of the result.
    public ConfigurationUnitResultSource ResultSource { get; set; }
}

public class OpenConfigurationSetResult
{
    public OpenConfigurationSetResult()
    {
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

    // The type of the unit being configured; not a name for this instance.
    public string? Type { get; set; }

    // The identifier name of this instance within the set.
    public string? Identifier { get; set; }

    // The current state of the configuration unit.
    public ConfigurationUnitState State { get; set; }

    // Determines if this configuration unit should be treated as a group.
    // A configuration unit group treats its `Settings` as the definition of child units.
    public bool IsGroup { get; set; }

    // The configuration units that are part of this unit (if IsGroup is true).
    public List<ConfigurationUnit>? Units { get; set; }

    // Contains the values that are for use by the configuration unit itself.
    public Dictionary<string, string>? Settings { get; set; }

    // Describes how this configuration unit will be used.
    public ConfigurationUnitIntent Intent { get; set; }
}

public class ConfigurationSetChangeData
{
    public ConfigurationSetChangeData()
    {
    }

    // The change event type that occurred.
    public ConfigurationSetChangeEventType Change { get; set; }

    // The state of the configuration set for this event (the ConfigurationSet can be used to get the current state, which may be different).
    public ConfigurationSetState SetState { get; set; }

    // The state of the configuration unit for this event (the ConfigurationUnit can be used to get the current state, which may be different).
    public ConfigurationUnitState UnitState { get; set; }

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

    // Results for each configuration unit in the set.
    public List<ApplyConfigurationUnitResult>? UnitResults { get; set; }

    // The overall result from applying the configuration set.
    public int ResultCode { get; set; }
}

#pragma warning restore SA1402 // File may only contain a single type
