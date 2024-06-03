// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.SetupFlow;

public enum CloneLocationKind
{
    DevDrive,
    LocalPath,
}

[EventData]
public class SwitchedCloningLocationEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServiceUsage;

    public CloneLocationKind SwitchedTo { get; }

    public bool IsNewDevDrive { get; }

    public bool IsDevDriveCustomized { get; }

    public SwitchedCloningLocationEvent(CloneLocationKind kindSwitchedTo, bool isNewDevDrive = false, bool isDevDriveCustomized = false)
    {
        SwitchedTo = kindSwitchedTo;
        IsNewDevDrive = isNewDevDrive;
        IsDevDriveCustomized = isDevDriveCustomized;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }
}
