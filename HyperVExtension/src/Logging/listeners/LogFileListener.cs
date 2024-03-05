// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
            var options = new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Append,
                Share = FileShare.ReadWrite,
            };
            writer = TextWriter.Synchronized(new StreamWriter(filename, options));
        }
        catch (IOException)
        {
            // Do nothing, we don't want to crash the program because
            // the log file couldn't be written, carry on without it.
        }
    }

    public override void HandleLogEvent(LogEvent logEvent)
    {
        HandleLogFileEvent(logEvent, true);
    }

    private void HandleLogFileEvent(LogEvent logEvent, bool newline)
    {
        HandleLogFileEvent(logEvent, newline, LogEvent.NoElapsed);
    }

    private void HandleLogFileEvent(LogEvent logEvent, bool newline, TimeSpan elapsed)
    {
        if (!MeetsFilter(logEvent))
        {
            return;
        }

        writer?.Write($"[{logEvent.Created:yyyy/MM/dd hh\\:mm\\:ss\\.ffff}][{logEvent.FullSourceName}] {logEvent.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}: {logEvent.Message}");
        if (elapsed != LogEvent.NoElapsed)
        {
            writer?.Write($" [Elapsed: {elapsed:hh\\:mm\\:ss\\.ffffff}]");
        }

        if (logEvent.Exception != null)
        {
            WriteLine(newline);
            writer?.Write(logEvent?.Exception.ToString());
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

    private bool MeetsFilter(LogEvent logEvent) => logEvent?.Severity >= Options?.LogFileFilter;

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
