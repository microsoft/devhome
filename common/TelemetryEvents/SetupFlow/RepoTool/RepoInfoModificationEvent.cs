// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.RepoTool;

public class RepoInfoModificationEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ModificationType
    {
        get;
    }

    public RepoInfoModificationEvent(string modificationType)
    {
        ModificationType = modificationType;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
