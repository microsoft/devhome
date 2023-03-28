// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;

namespace DevHome.Logging.Listeners;
public class LogFileListener : ListenerBase, IDisposable
{
    private readonly TextWriter? writer;

    public LogFileListener(string name, string filename)
        : base(name)
    {
        // Should handle locked file situation better.
        // For now assume one process is writing each file.
        // And fail silently if we can't write for whatever reason.
        try
        {
            writer = new StreamWriter(filename, true);
        }
        catch (IOException)
        {
            // Do nothing, we don't want to crash the program because
            // the log file couldn't be written, carry on without it.
        }
    }

    public override void HandleLogEvent(LogEvent evt)
    {
        HandleLogFileEvent(evt, true);
    }

    private void HandleLogFileEvent(LogEvent evt, bool newline)
    {
        HandleLogFileEvent(evt, newline, LogEvent.NoElapsed);
    }

    private void HandleLogFileEvent(LogEvent evt, bool newline, TimeSpan elapsed)
    {
        if (!MeetsFilter(evt))
        {
            return;
        }

        writer?.Write($"[{DateTime.UtcNow:yyyy/MM/dd hh\\:mm\\:ss\\.ffff}][{evt.FullSourceName}] {evt.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}: {evt.Message}");
        if (elapsed != LogEvent.NoElapsed)
        {
            writer?.Write($" [Elapsed: {elapsed:hh\\:mm\\:ss\\.ffffff}]");
        }

        if (evt.Exception != null)
        {
            WriteLine(newline);
            writer?.Write(evt?.Exception.ToString());
        }

        if (newline)
        {
            writer?.WriteLine();
        }

        writer?.Flush();
    }

    private void WriteLine(bool newline)
    {
        if (newline)
        {
            writer?.WriteLine();
        }
    }

    private bool MeetsFilter(LogEvent evt) => evt?.Severity >= Options?.LogFileFilter;

    private bool disposed; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            writer?.Dispose();
        }

        disposed = true;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
