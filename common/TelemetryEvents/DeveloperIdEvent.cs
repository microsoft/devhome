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

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class DeveloperIdEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public string developerId
    {
        get;
    }
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public DeveloperIdEvent(string providerName, IDeveloperId devId)
    {
        this.developerId = DeveloperIdHelper.GetHashedDeveloperId(providerName, devId);
    }

    public DeveloperIdEvent(string providerName, IEnumerable<IDeveloperId> devIds)
    {
        this.developerId = string.Join(" , ", devIds.Select(devId => DeveloperIdHelper.GetHashedDeveloperId(providerName, devId)));
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
