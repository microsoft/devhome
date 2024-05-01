// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Telemetry;

namespace WindowsSandboxExtension.Telemetry;

internal sealed class TraceLogging
{
    private const string ProviderName = "Microsoft.Windows.Containers.WindowsSandboxExtension";
    private const string StartingEventName = "StartingWindowsSandbox";
    private const string ExceptionThrownEventName = "ExceptionThrown";

    private static readonly TelemetryEventSource EventSource = new(ProviderName);

    public static void StartingWindowsSandbox()
    {
        var options = TelemetryEventSource.MeasuresOptions();
        options.Level = EventLevel.Informational;

        EventSource.Write(StartingEventName, options);
    }

    public static void ExceptionThrown(Exception exception)
    {
        var options = TelemetryEventSource.MeasuresOptions();
        options.Level = EventLevel.Error;

        EventSource.Write(
            ExceptionThrownEventName,
            options,
            new
            {
                name = exception.GetType().Name,
                stackTrace = exception.StackTrace,
                innerName = exception.InnerException?.GetType().Name,
                innerMessage = exception.InnerException?.Message,
                innerStackTrace = exception.InnerException?.ToString(),
                message = exception.Message,
            });
    }
}
