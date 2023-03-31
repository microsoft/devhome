// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Logging.Helpers;
using DevHome.Logging.Listeners;

namespace DevHome.Logging;
public class Logger : ILoggerHost, IDisposable
{
    public Logger(string name, Options options)
    {
        Name = name;
        Options = options;

        // Debug Listneer
        if (options.DebugListenerEnabled)
        {
            var debugListener = new DebugListener("Debug");
            AddListener(debugListener);
        }

        // Console listener
        if (options.LogStdoutEnabled)
        {
            var stdoutListener = new StdoutListener("Stdout");
            AddListener(stdoutListener);
        }

        // Log to file listener
        if (options.LogFileEnabled)
        {
            var logFilename = FileSystem.BuildOutputFilename(options.LogFileName, options.LogFileFolderPath);
            var logFileListener = new LogFileListener("LogFile", logFilename);
            ReportInfo($"Logging to {logFilename}");
            AddListener(logFileListener);
        }
    }

    ~Logger()
    {
        Dispose();
    }

    public Dictionary<string, IListener> Listeners { get; } = new Dictionary<string, IListener>();

    public Options Options
    {
        get;
    }

    public string Name
    {
        get;
    }

    public void AddListener(IListener listener)
    {
        listener.Host = this;
        Listeners.Add(listener.Name, listener);
    }

    public void ReportEvent(LogEvent evt)
    {
        ReportEventNoFailFast(evt);
        FailFastIfMeetsFailFastSeverity(evt);
    }

    private void ReportEventNoFailFast(LogEvent evt)
    {
        foreach (var listener in Listeners)
        {
            try
            {
                listener.Value.HandleLogEvent(evt);
            }
            catch
            {
                // Do not take down the entire app if a listener fails to log; ignore it.
#if DEBUG
                // Throw on debug builds.
                throw;
#endif
            }
        }
    }

    private void FailFastIfMeetsFailFastSeverity(LogEvent evt)
    {
        if (FailFast.IsFailFastSeverityLevel(evt.Severity, Options.FailFastSeverity))
        {
            // Send a final critical event indicating we are intentionally failing fast here.
            var failFastNotice = LogEvent.Create(
                Name,
                null!,
                SeverityLevel.Critical,
                $"Terminating program: failure event meets FailFast threshold of '{Options.FailFastSeverity}'.\n{Environment.StackTrace}");
            ReportEventNoFailFast(failFastNotice);
            Environment.FailFast(evt.Message, evt.Exception);
        }
    }

    public void ReportEvent(SeverityLevel severity, string message)
    {
        ReportEvent(severity, message, null!);
    }

    public void ReportEvent(SeverityLevel severity, string message, Exception exception)
    {
        var evt = LogEvent.Create(Name, null!, severity, message, exception);
        ReportEvent(evt);
    }

    public void ReportEvent(string component, SeverityLevel severity, string message)
    {
        ReportEvent(component, null!, severity, message, null!);
    }

    public void ReportEvent(string component, SeverityLevel severity, string message, Exception exception)
    {
        ReportEvent(component, null!, severity, message, exception);
    }

    public void ReportEvent(string component, string subComponent, SeverityLevel severity, string message)
    {
        ReportEvent(component, subComponent, severity, message, null!);
    }

    public void ReportEvent(string component, string subComponent, SeverityLevel severity, string message, System.Exception exception)
    {
        var evt = LogEvent.Create(component, subComponent, severity, message, exception);
        ReportEvent(evt);
    }

    public void ReportDebug(string message)
    {
        ReportEvent(SeverityLevel.Debug, message);
    }

    public void ReportDebug(string message, Exception exception)
    {
        ReportEvent(SeverityLevel.Debug, message, exception);
    }

    public void ReportDebug(string component, string message)
    {
        ReportEvent(component, SeverityLevel.Debug, message);
    }

    public void ReportDebug(string component, string message, Exception exception)
    {
        ReportEvent(component, SeverityLevel.Debug, message, exception);
    }

    public void ReportDebug(string component, string subComponent, string message)
    {
        ReportEvent(component, subComponent, SeverityLevel.Debug, message);
    }

    public void ReportDebug(string component, string subComponent, string message, Exception exception)
    {
        ReportEvent(component, subComponent, SeverityLevel.Debug, message, exception);
    }

    public void ReportInfo(string message)
    {
        ReportEvent(SeverityLevel.Info, message);
    }

    public void ReportInfo(string message, Exception exception)
    {
        ReportEvent(SeverityLevel.Info, message, exception);
    }

    public void ReportInfo(string component, string message)
    {
        ReportEvent(component, SeverityLevel.Info, message);
    }

    public void ReportInfo(string component, string message, Exception exception)
    {
        ReportEvent(component, SeverityLevel.Info, message, exception);
    }

    public void ReportInfo(string component, string subComponent, string message)
    {
        ReportEvent(component, subComponent, SeverityLevel.Info, message);
    }

    public void ReportInfo(string component, string subComponent, string message, Exception exception)
    {
        ReportEvent(component, subComponent, SeverityLevel.Info, message, exception);
    }

    public void ReportWarn(string message)
    {
        ReportEvent(SeverityLevel.Warn, message);
    }

    public void ReportWarn(string message, Exception exception)
    {
        ReportEvent(SeverityLevel.Warn, message, exception);
    }

    public void ReportWarn(string component, string message)
    {
        ReportEvent(component, SeverityLevel.Warn, message);
    }

    public void ReportWarn(string component, string message, Exception exception)
    {
        ReportEvent(component, SeverityLevel.Warn, message, exception);
    }

    public void ReportWarn(string component, string subComponent, string message)
    {
        ReportEvent(component, subComponent, SeverityLevel.Warn, message);
    }

    public void ReportWarn(string component, string subComponent, string message, Exception exception)
    {
        ReportEvent(component, subComponent, SeverityLevel.Warn, message, exception);
    }

    public void ReportError(string message)
    {
        ReportEvent(SeverityLevel.Error, message);
    }

    public void ReportError(string message, Exception exception)
    {
        ReportEvent(SeverityLevel.Error, message, exception);
    }

    public void ReportError(string component, string message)
    {
        ReportEvent(component, SeverityLevel.Error, message);
    }

    public void ReportError(string component, string message, Exception exception)
    {
        ReportEvent(component, SeverityLevel.Error, message, exception);
    }

    public void ReportError(string component, string subComponent, string message)
    {
        ReportEvent(component, subComponent, SeverityLevel.Error, message);
    }

    public void ReportError(string component, string subComponent, string message, Exception exception)
    {
        ReportEvent(component, subComponent, SeverityLevel.Error, message, exception);
    }

    public void ReportCritical(string message)
    {
        ReportEvent(SeverityLevel.Critical, message);
    }

    public void ReportCritical(string message, Exception exception)
    {
        ReportEvent(SeverityLevel.Critical, message, exception);
    }

    public void ReportCritical(string component, string message)
    {
        ReportEvent(component, SeverityLevel.Critical, message);
    }

    public void ReportCritical(string component, string message, Exception exception)
    {
        ReportEvent(component, SeverityLevel.Critical, message, exception);
    }

    public void ReportCritical(string component, string subComponent, string message)
    {
        ReportEvent(component, subComponent, SeverityLevel.Critical, message);
    }

    public void ReportCritical(string component, string subComponent, string message, Exception exception)
    {
        ReportEvent(component, subComponent, SeverityLevel.Critical, message, exception);
    }

    private bool disposed; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            var disposingEvent = LogEvent.Create(Name, null!, SeverityLevel.Debug, "Disposing of all logging listeners.");
            ReportEvent(disposingEvent);

            if (disposing)
            {
                Listeners.DisposeAll();
            }

            disposed = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
