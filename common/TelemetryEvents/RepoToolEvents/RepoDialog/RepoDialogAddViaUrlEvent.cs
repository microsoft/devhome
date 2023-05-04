// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepoToolEvents.RepoDialog;

[EventData]
public class RepoDialogAddViaUrlEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public string ProviderName
    {
        get;
    }

    public bool FoundProvider
    {
        get;
    }

    public bool FoundRepo
    {
        get;
    }

    public RepoDialogAddViaUrlEvent(string providerName, bool foundProvider, bool foundRepo)
    {
        ProviderName = providerName;
        FoundProvider = foundProvider;
        FoundRepo = foundRepo;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
