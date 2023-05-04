// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.RepoToolEvents;

[EventData]
public class CloneToDevDriveEvent : EventBase
{
    public bool IsExistingDevDrive { get; private set; }

    public bool IsNewDevDrive { get; private set; }

    public bool DidUserCustomizeNewDevDrive { get; private set; }

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    private CloneToDevDriveEvent()
    {
    }

    public static CloneToDevDriveEvent MadeNewDevDrive(bool didCustomizeNewDevDrive)
    {
        return new CloneToDevDriveEvent { DidUserCustomizeNewDevDrive = didCustomizeNewDevDrive, IsNewDevDrive = true };
    }

    public static CloneToDevDriveEvent UsedExistingDevDrive()
    {
        return new CloneToDevDriveEvent { IsExistingDevDrive = true };
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace
    }
}
