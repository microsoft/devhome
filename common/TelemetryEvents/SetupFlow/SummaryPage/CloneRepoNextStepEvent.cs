// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;

[EventData]
public class CloneRepoNextStepEvent : EventBase
{
    public string Operation { get; }

    public string RepoName { get; }

    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public CloneRepoNextStepEvent(string operation, string repoName)
    {
        Operation = operation;
        RepoName = repoName;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // no op
    }
}
