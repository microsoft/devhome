// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

    public override void HandleLogEvent(LogEvent logEvent)
    {
        ConsoleHandleLogEvent(logEvent, true);
    }

    private void ConsoleHandleLogEvent(LogEvent logEvent, bool newline)
    {
        ConsoleHandleLogEvent(logEvent, newline, LogEvent.NoElapsed);
    }

    private void ConsoleHandleLogEvent(LogEvent logEvent, bool newline, TimeSpan elapsed)
    {
        if (!MeetsFilter(logEvent))
        {
            return;
        }

        var line = new List<Tuple<ConsoleColor, string>>
        {
            Tuple.Create(CDefaultColor, "["),
            Tuple.Create(CSourceColor, (logEvent.SubSource != null) ? $"{logEvent.Source}/{logEvent.SubSource}" : $"{logEvent.Source}"),
            Tuple.Create(CDefaultColor, "] "),
            Tuple.Create(GetSeverityColor(logEvent.Severity), logEvent.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)),
            Tuple.Create(CDefaultColor, ": "),
            Tuple.Create(GetSeverityColor(logEvent.Severity), logEvent.Message),
        };

        if (elapsed != LogEvent.NoElapsed)
        {
            line.Add(Tuple.Create(CDefaultColor, " ["));
            line.Add(Tuple.Create(CElapsedColor, $"Elapsed: {elapsed:hh\\:mm\\:ss\\.ffffff}"));
            line.Add(Tuple.Create(CDefaultColor, "]"));
        }

        if (logEvent.Exception != null)
        {
            line.Add(Tuple.Create(CExceptionColor, $"{Environment.NewLine}{logEvent.Exception}"));
        }

        if (newline)
        {
            line.Add(Tuple.Create(CDefaultColor, Environment.NewLine));
        }

        WriteColor(line);
        Console.ResetColor();
        Console.Out.Flush();
    }

    private bool MeetsFilter(LogEvent logEvent)
    {
        return logEvent.Severity >= Options?.LogStdoutFilter;
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
