// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

[EventData]
public class RepoDialogGetAccountEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string ProviderName
    {
        get;
    }

    public bool AlreadyLoggedIn
    {
        get;
    }

    public RepoDialogGetAccountEvent(string providerName, bool alreadyLoggedIn)
    {
        ProviderName = providerName;
        AlreadyLoggedIn = alreadyLoggedIn;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
