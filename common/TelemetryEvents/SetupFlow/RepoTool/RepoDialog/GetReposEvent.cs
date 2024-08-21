// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Configuration.Provider;
using System.Diagnostics.Tracing;
using System.Security.Cryptography.X509Certificates;
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

    public int HResult { get; }

    public string ExceptionMessage { get; } = string.Empty;

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

    public GetReposEvent(Exception exception, string providerName, IDeveloperId developerId)
    {
        StageName = "Error";
        ProviderName = providerName;
        DeveloperId = developerId is null ? string.Empty : DeveloperIdHelper.GetHashedDeveloperId(providerName, developerId);
        ExceptionMessage = exception.Message;
        HResult = exception.HResult;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive string is the developerID.  GetHashedDeveloperId is used to hash the developerId.
    }
}
