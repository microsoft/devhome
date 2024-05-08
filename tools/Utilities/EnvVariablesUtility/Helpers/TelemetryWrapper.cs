// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.EnvironmentVariables.TelemetryEvents;
using DevHome.Telemetry;
using EnvironmentVariablesUILib.Models;

namespace DevHome.EnvironmentVariables.Helpers;

public class TelemetryWrapper : EnvironmentVariablesUILib.Telemetry.ITelemetry
{
    public void LogEnvironmentVariablesProfileEnabledEvent(bool enabled)
    {
        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("EnvironmentVariablesApp_EnvironmentVariablesProfileEnabled", LogLevel.Measure, new EnvironmentVariablesProfileEnabledEvent(enabled), null);
    }

    public void LogEnvironmentVariablesVariableChangedEvent(VariablesSetType type)
    {
        TelemetryFactory.Get<DevHome.Telemetry.ITelemetry>().Log("EnvironmentVariablesApp_EnvironmentVariablesVariableChanged", LogLevel.Measure, new EnvironmentVariablesVariableChangedEvent(type), null);
    }
}
