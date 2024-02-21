// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Storage;

namespace DevHome.Logging;

/// <summary>
/// A logger for an individual component; each component's logs go to a different file.
/// </summary>
public class ComponentLogger : IDisposable
{
    private readonly string _componentName;
    private readonly string _folderName;
    private Logger? _logger;
    private bool disposedValue;

    public ComponentLogger(string componentName)
        : this(componentName, componentName)
    {
    }

    public ComponentLogger(string componentName, string folderName)
    {
        _componentName = componentName;
        _folderName = folderName;
    }

    public Logger? Logger
    {
        get
        {
            try
            {
                _logger ??= new Logger(_componentName, GetLoggingOptions());
            }
            catch
            {
                // Do nothing if logger fails.
            }

            return _logger;
        }
    }

    public void Attach(Logger logger)
    {
        if (logger is not null)
        {
            _logger?.Dispose();
            _logger = logger;
        }
    }

    private Options GetLoggingOptions()
    {
        return new Options
        {
            LogFileFolderRoot = ApplicationData.Current.TemporaryFolder.Path,
            LogFileName = _componentName + "_{now}.log",
            LogFileFolderName = _folderName,
            DebugListenerEnabled = true,
#if DEBUG
            LogStdoutEnabled = true,
            LogStdoutFilter = SeverityLevel.Debug,
            LogFileFilter = SeverityLevel.Debug,
#else
            LogStdoutEnabled = false,
            LogStdoutFilter = SeverityLevel.Info,
            LogFileFilter = SeverityLevel.Info,
#endif
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _logger?.Dispose();
                _logger = null;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
