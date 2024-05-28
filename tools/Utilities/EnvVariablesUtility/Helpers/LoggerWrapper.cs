// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Serilog;

namespace DevHome.EnvironmentVariables.Helpers;

internal sealed class LoggerWrapper : EnvironmentVariablesUILib.Helpers.ILogger
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(EnvironmentVariablesApp));

    public void LogDebug(string message)
    {
        _log.Debug(message);
    }

    public void LogError(string message)
    {
        _log.Error(message);
    }

    public void LogError(string message, Exception ex)
    {
        _log.Error(ex, message);
    }

    public void LogInfo(string message)
    {
        _log.Information(message);
    }

    public void LogTrace()
    {
        _log.Verbose("Trace");
    }

    public void LogWarning(string message)
    {
        _log.Warning(message);
    }
}
