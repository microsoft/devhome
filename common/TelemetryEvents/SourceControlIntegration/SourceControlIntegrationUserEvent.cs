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

namespace DevHome.Common.TelemetryEvents.SourceControlIntegration;

[EventData]

public class SourceControlIntegrationUserEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string RepositoryRootPath
    {
        get;
    }

    public SourceControlIntegrationUserEvent(string sourceControlProviderClassId, string repositoryRootPath)
    {
        RepositoryRootPath = SourceControlIntegrationHelper.GetSafeRootPath(repositoryRootPath);
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings is the repository root path. GetSafeRootPath is used to hash the root path.
    }
}
