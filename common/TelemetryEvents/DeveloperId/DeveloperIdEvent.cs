// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.DeveloperId;

[EventData]
public class DeveloperIdEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

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
