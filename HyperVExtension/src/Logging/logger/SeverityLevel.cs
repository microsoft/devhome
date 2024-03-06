// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging;

public enum SeverityLevel
{
    Debug,
    Info,
    Warn,
    Error,
    Critical,
}

// For setting fail-fast behavior, at what level of failure will we conduct a fail-fast?
// This is mostly intended for specifying whether any error or any critical error causes an FailFast.
// By default we assume any critical failure is by definition something we should not continue after detecting.
public enum FailFastSeverityLevel
{
    Ignore = -1,
    Warning = SeverityLevel.Warn,
    Error = SeverityLevel.Error,
    Critical = SeverityLevel.Critical,
}

#pragma warning disable SA1649 // File name should match first type name. TODO: rename or remove file.
public class FailFast
#pragma warning restore SA1649 // File name should match first type name
{
    public static bool IsFailFastSeverityLevel(SeverityLevel severity, FailFastSeverityLevel failFastSeverity)
    {
        return failFastSeverity switch
        {
            FailFastSeverityLevel.Warning => severity >= SeverityLevel.Warn,
            FailFastSeverityLevel.Error => severity >= SeverityLevel.Error,
            FailFastSeverityLevel.Critical => severity >= SeverityLevel.Critical,
            _ => false,
        };
    }
}
