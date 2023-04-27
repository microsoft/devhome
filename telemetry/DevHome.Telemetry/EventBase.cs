// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Telemetry;

/// <summary>
/// Base class for all telemetry events to ensure they are properly tagged.
/// </summary>
/// <remarks>
/// The public properties of each event are logged in the telemetry.
/// We should not change an event's properties, as that could break the processing of that event's data.
/// </remarks>
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
