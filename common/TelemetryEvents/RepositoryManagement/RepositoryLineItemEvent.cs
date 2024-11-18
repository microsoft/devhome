// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepositoryManagement;

[EventData]
public class RepositoryLineItemEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServiceUsage;

    public string Action { get; } = string.Empty;

    public string RepositoryName { get; } = string.Empty;

    public RepositoryLineItemEvent(string action, string repositoryName)
    {
        Action = action;
        RepositoryName = repositoryName;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // no sensitive strings to replace
    }
}
