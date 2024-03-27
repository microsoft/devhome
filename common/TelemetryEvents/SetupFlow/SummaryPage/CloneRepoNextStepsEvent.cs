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

namespace DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;

[EventData]
public class CloneRepoNextStepsEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public int NumberOfConfigurationFilesFound { get; }

    public CloneRepoNextStepsEvent(int numberOfConfigurationFilesFound)
    {
        NumberOfConfigurationFilesFound = numberOfConfigurationFilesFound;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No op.
    }
}
