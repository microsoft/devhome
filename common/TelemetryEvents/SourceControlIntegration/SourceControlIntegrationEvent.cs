// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SourceControlIntegration;

[EventData]
public class SourceControlIntegrationEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string RepositoryRootPath
    {
        get;
    }

    public int TrackedRepositoryCount
    {
        get;
    }

    public SourceControlIntegrationEvent(string sourceControlProviderClassId, string repositoryRootPath, int trackedRepositoryCount)
    {
        RepositoryRootPath = SourceControlIntegrationHelper.GetSafeRootPath(repositoryRootPath);
        TrackedRepositoryCount = trackedRepositoryCount;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings is the repository root path. GetSafeRootPath is used to potentially remove PII and
        // keep last part of path.
    }
}
