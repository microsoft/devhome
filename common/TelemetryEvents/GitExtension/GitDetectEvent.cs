// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;

namespace DevHome.Common.TelemetryEvents.GitExtension;

// How git.exe was located
public enum GitDetectStatus
{
    // git.exe was not found on the system
    NotFound,

    // In the PATH environment variable
    PathEnvironmentVariable,

    // Probed well-known registry keys to find a Git install location
    RegistryProbe,

    // Probed well-known folders under Program Files [(x86)]
    ProgramFiles,
}

[EventData]
public class GitDetectEvent : EventBase
{
    public override PartA_PrivTags PartA_PrivTags => PrivTags.ProductAndServicePerformance;

    public string Status { get; }

    public string Version { get; }

    public GitDetectEvent(GitDetectStatus status, string version)
    {
        Status = status.ToString();
        Version = version;
    }

    public override void ReplaceSensitiveStrings(Func<string, string> replaceSensitiveStrings)
    {
        // This event so far has no sensitive strings
    }
}
