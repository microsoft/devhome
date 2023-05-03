// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.TelemetryEvents;

[EventData]
public class DeveloperIdEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public string developerId
    {
        get;
    }
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public DeveloperIdEvent(string providerName, IDeveloperId devId)
    {
        this.developerId = GetHashedDeveloperId(providerName, devId);
    }

    public DeveloperIdEvent(string providerName, IEnumerable<IDeveloperId> devIds)
    {
        this.developerId = string.Join(" , ", devIds.Select(devId => GetHashedDeveloperId(providerName, devId)));
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // The only sensitive strings are the dev IDs, but we already hashed them
    }

    private static string GetHashedDeveloperId(string providerName, IDeveloperId devId)
    {
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
