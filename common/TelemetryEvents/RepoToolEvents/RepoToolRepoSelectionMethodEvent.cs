// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepoToolEvents;

[EventData]
public class RepoToolRepoSelectionMethodEvent : EventBase
{
    public bool DidUserAddViaUrl { get; private set; }

    public bool DidUserAddViaAccount { get; private set; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    private RepoToolRepoSelectionMethodEvent()
    {
    }

    public static RepoToolRepoSelectionMethodEvent AddedViaUrl()
    {
        return new RepoToolRepoSelectionMethodEvent { DidUserAddViaUrl = true, };
    }

    public static RepoToolRepoSelectionMethodEvent AddedViaAccount()
    {
        return new RepoToolRepoSelectionMethodEvent { DidUserAddViaAccount = true, };
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
