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

namespace DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;

[EventData]
public class CloneRepoNextStepError : EventBase
{
    public string Operation { get; }

    public Exception Error { get; }

    public string RepoName { get; }

    public override PartA_PrivTags PartA_PrivTags => PartA_PrivTags.ProductAndServicePerformance;

    public CloneRepoNextStepError(string operation, Exception exception, string repoName)
    {
        Operation = operation;
        Error = exception;
        RepoName = repoName;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // no op
    }
}
