// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;

namespace DevHome.Logging.Listeners;
public class StdoutListener : ListenerBase
{
    private static readonly ConsoleColor CDefaultColor = ConsoleColor.White;
    private static readonly ConsoleColor CDebugColor = ConsoleColor.DarkGray;
    private static readonly ConsoleColor CInfoColor = ConsoleColor.White;
    private static readonly ConsoleColor CWarnColor = ConsoleColor.Yellow;
    private static readonly ConsoleColor CErrorColor = ConsoleColor.Red;
    private static readonly ConsoleColor CCriticalColor = ConsoleColor.Magenta;
    private static readonly ConsoleColor CExceptionColor = ConsoleColor.Red;
    private static readonly ConsoleColor CElapsedColor = ConsoleColor.Green;
    private static readonly ConsoleColor CSourceColor = ConsoleColor.Cyan;

    public StdoutListener(string name)
        : base(name)
    {
    }

    public override void HandleLogEvent(LogEvent evt)
    {
        ConsoleHandleLogEvent(evt, true);
    }

    private void ConsoleHandleLogEvent(LogEvent evt, bool newline)
    {
        ConsoleHandleLogEvent(evt, newline, LogEvent.NoElapsed);
    }

    private void ConsoleHandleLogEvent(LogEvent evt, bool newline, TimeSpan elapsed)
    {
        if (!MeetsFilter(evt))
        {
            return;
        }

        var line = new List<Tuple<ConsoleColor, string>>
        {
            Tuple.Create(CDefaultColor, "["),
            Tuple.Create(CSourceColor, (evt.SubSource != null) ? $"{evt.Source}/{evt.SubSource}" : $"{evt.Source}"),
            Tuple.Create(CDefaultColor, "] "),
            Tuple.Create(GetSeverityColor(evt.Severity), evt.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)),
            Tuple.Create(CDefaultColor, ": "),
            Tuple.Create(GetSeverityColor(evt.Severity), evt.Message),
        };

        if (elapsed != LogEvent.NoElapsed)
        {
            line.Add(Tuple.Create(CDefaultColor, " ["));
            line.Add(Tuple.Create(CElapsedColor, $"Elapsed: {elapsed:hh\\:mm\\:ss\\.ffffff}"));
            line.Add(Tuple.Create(CDefaultColor, "]"));
        }

        if (evt.Exception != null)
        {
            line.Add(Tuple.Create(CExceptionColor, $"{Environment.NewLine}{evt.Exception}"));
        }

        if (newline)
        {
            line.Add(Tuple.Create(CDefaultColor, Environment.NewLine));
        }

        WriteColor(line);
        Console.ResetColor();
        Console.Out.Flush();
    }

    private bool MeetsFilter(LogEvent evt)
    {
        return evt.Severity >= Options?.LogStdoutFilter;
    }

    private void WriteColor(List<Tuple<ConsoleColor, string>> strings)
    {
        if (strings == null)
        {
            return;
        }

        foreach (var s in strings)
        {
            WriteColor(s);
        }
    }

    private void WriteColor(Tuple<ConsoleColor, string> s)
    {
        if (Console.IsOutputRedirected)
        {
            Console.Write(s.Item2);
        }
        else
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = s.Item1;
            Console.Write(s.Item2);
            Console.ForegroundColor = currentColor;
        }
    }

    private ConsoleColor GetSeverityColor(SeverityLevel severity)
    {
        return severity switch
        {
            SeverityLevel.Debug => CDebugColor,
            SeverityLevel.Info => CInfoColor,
            SeverityLevel.Warn => CWarnColor,
            SeverityLevel.Error => CErrorColor,
            SeverityLevel.Critical => CCriticalColor,
            _ => CDefaultColor,
        };
    }
}
