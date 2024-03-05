// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DevHome.Logging.Listeners;

public class DebugListener : ListenerBase
{
    public DebugListener(string name)
        : base(name)
    {
    }

    public override void HandleLogEvent(LogEvent logEvent)
    {
        // This listener does nothing unless a Debugger is attached.
        // All events will be sent to the debugger.
        if (Debugger.IsAttached)
        {
            DebugHandleLogEvent(logEvent);
        }
    }

    private void DebugHandleLogEvent(LogEvent logEvent)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"[{logEvent.FullSourceName}] {logEvent.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}: {logEvent.Message}");
        if (logEvent.Exception != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{Environment.NewLine}{logEvent.Exception}");
        }

        Trace.WriteLine(sb.ToString());
    }
}
