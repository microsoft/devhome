// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class EnvironmentRedirectionUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string NavigationAction { get; }

    public string OriginPage { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentRedirectionUserEvent"/> class.
    /// </summary>
    public EnvironmentRedirectionUserEvent(string navigationAction, string originPage)
    {
        NavigationAction = navigationAction;
        OriginPage = originPage;
    }

    // Inherited but unused.
    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
    }
}
