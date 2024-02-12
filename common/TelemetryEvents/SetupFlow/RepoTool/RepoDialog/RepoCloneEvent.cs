// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class RepoCloneEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string ProviderName
    {
        get;
    }

    public string DeveloperId
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RepoCloneEvent"/> class.
    /// </summary>
    /// <param name="providerName">Name of the provider to use to clone the repo</param>
    /// <param name="developerId">The account to use for private/org repos.  Can be null</param>
    public RepoCloneEvent(string providerName, IDeveloperId developerId)
    {
        ProviderName = providerName;

        DeveloperId = developerId is null ? string.Empty : DeveloperIdHelper.GetHashedDeveloperId(providerName, developerId);
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
