// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.DeveloperId;

// This is an event for things which are not direct user actions.
[EventData]
public class DeveloperIdEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string DeveloperId
    {
        get;
    }

    public DeveloperIdEvent(string providerName, IDeveloperId devId)
    {
        DeveloperId = DeveloperIdHelper.GetHashedDeveloperId(providerName, devId);
    }

    public DeveloperIdEvent(string providerName, IEnumerable<IDeveloperId> devIds)
    {
        DeveloperId = string.Join(" , ", devIds.Select(devId => DeveloperIdHelper.GetHashedDeveloperId(providerName, devId)));
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
