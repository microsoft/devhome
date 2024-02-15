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

// By default, we FailFast on Critical since Critical failure is, by definition, something we should not continue after detecting.
public enum FailFastSeverityLevel
{
    Ignore = -1,
    Warning = SeverityLevel.Warn,
    Error = SeverityLevel.Error,
    Critical = SeverityLevel.Critical,
}

public class FailFast
{
    /// <summary>
    /// Determine whether the message's severity level is at least at the FailFastSeverity threshold.
    /// </summary>
    /// <param name="severity">Severity of the log message.</param>
    /// <param name="failFastSeverity">Threshold set for FailFast on logging.</param>
    /// <returns>True if Dev Home should fail fast, false if it should not.</returns>
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
