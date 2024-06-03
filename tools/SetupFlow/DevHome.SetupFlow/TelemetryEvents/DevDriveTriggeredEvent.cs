// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Common.Models;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.SetupFlow.Common.TelemetryEvents;

[EventData]
internal sealed class DevDriveTriggeredEvent : EventBase
{
    public DevDriveTriggeredEvent(IDevDrive devDrive, long duration, int hr)
    {
        _devDrive = devDrive;
        errorCode = $"0x{hr:X}";
        this.duration = duration;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // No sensitive strings to replace.
    }

    private readonly IDevDrive _devDrive;

#pragma warning disable SA1300 // Element should begin with upper-case letter
    public long duration { get; }

    public string errorCode { get; }

    public ulong volumeSizeInBytes => _devDrive.DriveSizeInBytes;

    public uint diskMediaType => (uint)_devDrive.DriveMediaType;
#pragma warning restore SA1300 // Element should begin with upper-case letter

    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;
}
