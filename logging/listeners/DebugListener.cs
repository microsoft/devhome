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

    public override void HandleLogEvent(LogEvent evt)
    {
        // This listener does nothing unless a Debugger is attached.
        // All events will be sent to the debugger.
        if (Debugger.IsAttached)
        {
            DebugHandleLogEvent(evt);
        }
    }

    private void DebugHandleLogEvent(LogEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"[{evt.FullSourceName}] {evt.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}: {evt.Message}");
        if (evt.Exception != null)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{Environment.NewLine}{evt.Exception}");
        }

        Trace.WriteLine(sb.ToString());
    }
}
