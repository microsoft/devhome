// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents;

/// <summary>
/// Base class for all telemetry events to ensure they are properly tagged.
/// </summary>
[EventData]
public abstract class EventBase
{
    /// <summary>
    /// Gets the privacy datatype tag for the telemetry event.
    /// </summary>
    public abstract PartA_PrivTags PartA_PrivTags
    {
        get;
    }
}
