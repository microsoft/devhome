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
public class GetReposEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string StageName { get; }

    public string ProviderName { get; }

    public string DeveloperId { get; }

    public int NumberOfReposFound { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetReposEvent"/> class.
    /// </summary>
    /// <param name="stageName">Where this event falls in getting repositories</param>
    /// <param name="providerName">Name of the provider to use to clone the repo</param>
    /// <param name="developerId">The account to use for private/org repos.  Can be null</param>
    public GetReposEvent(string stageName, string providerName, IDeveloperId developerId)
    {
        StageName = stageName;
        ProviderName = providerName;
        DeveloperId = developerId is null ? string.Empty : DeveloperIdHelper.GetHashedDeveloperId(providerName, developerId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetReposEvent"/> class.
    /// </summary>
    /// <param name="stageName">Where this event falls in getting repositories</param>
    /// <param name="providerName">Name of the provider to use to clone the repo</param>
    /// <param name="developerId">The account to use for private/org repos.  Can be null</param>
    /// <param name="reposFound">The number of repos found</param>
    public GetReposEvent(string stageName, string providerName, IDeveloperId developerId, int reposFound)
    {
        StageName = stageName;
        ProviderName = providerName;
        DeveloperId = developerId is null ? string.Empty : DeveloperIdHelper.GetHashedDeveloperId(providerName, developerId);
        NumberOfReposFound = reposFound;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive string is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
