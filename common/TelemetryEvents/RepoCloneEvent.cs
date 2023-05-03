// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using System.Text;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.TelemetryEvents;

[EventData]
public class ReposCloneEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ProviderName
    {
        get;
    }

    public string DeveloperId
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReposCloneEvent"/> class.
    /// </summary>
    /// <param name="providerName">Name of the provider to use to clone the repo</param>
    /// <param name="developerId">The account to use for private/org repos.  Can be null</param>
    public ReposCloneEvent(string providerName, IDeveloperId developerId)
    {
        ProviderName = providerName;

        if (developerId == null)
        {
            DeveloperId = "N/A";
        }
        else
        {
            DeveloperId = GetHashedDeveloperId(providerName, developerId);
        }
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings are the dev IDs, but we already hashed them
    }

    private static string GetHashedDeveloperId(string providerName, IDeveloperId devId)
    {
        // I understand this is a duplicate from DeveloperIdEvent.
        // Currently trying to minimize the amount of platform code changes.
        // TODO: Move this logic to a helper file.
        // TODO: Instead of LoginId, hash a globally unique id of DeveloperId (like url)
        using var hasher = SHA256.Create();
        var loginIdBytes = Encoding.ASCII.GetBytes(devId.LoginId());
        var hashedLoginId = hasher.ComputeHash(loginIdBytes);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(hashedLoginId);
        }

        var hashedLoginIdString = BitConverter.ToString(hashedLoginId).Replace("-", string.Empty);

        return $"{hashedLoginIdString}_{providerName}";
    }
}
