// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
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
            AddListenerInternal(debugListener);
        }

        // Console listener
        if (options.LogStdoutEnabled)
        {
            var stdoutListener = new StdoutListener("Stdout");
            AddListenerInternal(stdoutListener);
        }

        // Log to file listener
        if (options.LogFileEnabled)
        {
            var logFilename = FileSystem.BuildOutputFilename(options.LogFileName, options.LogFileFolderPath);
            var logFileListener = new LogFileListener("LogFile", logFilename);
            ReportInfo($"Logging to {logFilename}");
            AddListenerInternal(logFileListener);
        }

        StartLogEventProcessor();
    }

    ~Logger()
    {
        Dispose();
    }

    private readonly BlockingCollection<LogEvent> eventQueue = new(new ConcurrentQueue<LogEvent>());

    private readonly ManualResetEvent processorCanceledEvent = new(true);

    private CancellationTokenSource? cancelTokenSource;

    private bool _logEventProcessorIsStopped = true;

    private ConcurrentDictionary<string, IListener> Listeners { get; } = new ConcurrentDictionary<string, IListener>();

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
        // External adds outside the constructor need to stop the log event processor.
        // Otherwise we could be adding while iterating through the list.
        StopLogEventProcessor();
        AddListenerInternal(listener);
        StartLogEventProcessor();
    }

    private void AddListenerInternal(IListener listener)
    {
        listener.Host = this;
        Listeners.TryAdd(listener.Name, listener);
    }

    public void ReportEvent(LogEvent logEvent)
    {
        try
        {
            _ = eventQueue.TryAdd(logEvent);
        }
        catch
        {
            // Errors trying to add to the log are ignored.
        }
    }

    private void StartLogEventProcessor()
    {
        _ = Task.Run(() =>
        {
            _logEventProcessorIsStopped = false;
            cancelTokenSource = new CancellationTokenSource();
            processorCanceledEvent.Reset();
            while (!eventQueue.IsCompleted)
            {
                LogEvent? logEvent;
                try
                {
                    _ = eventQueue.TryTake(out logEvent, -1, cancelTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // It is possible we miss events because cancellation occurred while there was
                    // still an event queue. Drain the remaining queue and process those events
                    // before terminating.
                    try
                    {
                        // This is a snapshot of the current collection, it will not block or handle
                        // new items added after this point.
                        foreach (var evt in eventQueue)
                        {
                            ProcessLogEvent(evt);
                        }
                    }
                    catch
                    {
                        // This is best effort, if there are problems, carry on.
                    }

                    _logEventProcessorIsStopped = true;
                    processorCanceledEvent.Set();
                    break;
                }

                if (logEvent is not null)
                {
                    ProcessLogEventFailFast(logEvent);
                }
            }
        });
    }

    private void StopLogEventProcessor()
    {
        if (_logEventProcessorIsStopped)
        {
            return;
        }

        try
        {
            cancelTokenSource?.Cancel();
        }
        catch
        {
            // if there is a problem cancelling the task, don't wait on it finishing.
            return;
        }

        // Give the logger at most five seconds to finish writing out events.
        processorCanceledEvent.WaitOne(5 * 1000);
    }

    private void ProcessLogEvent(LogEvent logEvent)
    {
        foreach (var listener in Listeners)
        {
            try
            {
                listener.Value.HandleLogEvent(logEvent);
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

    private void ProcessLogEventFailFast(LogEvent logEvent)
    {
        ProcessLogEvent(logEvent);
        FailFastIfMeetsFailFastSeverity(logEvent);
    }

    private void FailFastIfMeetsFailFastSeverity(LogEvent logEvent)
    {
        if (FailFast.IsFailFastSeverityLevel(logEvent.Severity, Options.FailFastSeverity))
        {
            // Send a final critical event indicating we are intentionally failing fast here.
            var failFastNotice = LogEvent.Create(
                Name,
                null!,
                SeverityLevel.Critical,
                $"Terminating program: failure event meets FailFast threshold of '{Options.FailFastSeverity}'.\n{Environment.StackTrace}");
            ProcessLogEvent(failFastNotice);
            Environment.FailFast(logEvent.Message, logEvent.Exception);
        }
    }

    public void ReportEvent(SeverityLevel severity, string message)
    {
        ReportEvent(severity, message, null!);
    }

    public void ReportEvent(SeverityLevel severity, string message, Exception exception)
    {
        var logEvent = LogEvent.Create(Name, null!, severity, message, exception);
        ReportEvent(logEvent);
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
        var logEvent = LogEvent.Create(component, subComponent, severity, message, exception);
        ReportEvent(logEvent);
    }

    public void ReportDebug(string message)
    {
#if DEBUG
        ReportEvent(SeverityLevel.Debug, message);
#endif
    }

    public void ReportDebug(string message, Exception exception)
    {
#if DEBUG
        ReportEvent(SeverityLevel.Debug, message, exception);
#endif
    }

    public void ReportDebug(string component, string message)
    {
#if DEBUG
        ReportEvent(component, SeverityLevel.Debug, message);
#endif
    }

    public void ReportDebug(string component, string message, Exception exception)
    {
#if DEBUG
        ReportEvent(component, SeverityLevel.Debug, message, exception);
#endif
    }

    public void ReportDebug(string component, string subComponent, string message)
    {
#if DEBUG
        ReportEvent(component, subComponent, SeverityLevel.Debug, message);
#endif
    }

    public void ReportDebug(string component, string subComponent, string message, Exception exception)
    {
#if DEBUG
        ReportEvent(component, subComponent, SeverityLevel.Debug, message, exception);
#endif
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
            StopLogEventProcessor();
            Listeners.DisposeAll();
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
