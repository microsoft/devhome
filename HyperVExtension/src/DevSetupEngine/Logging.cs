// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Logging;
using Windows.Storage;

namespace HyperVExtension.DevSetupEngine;

public class Logging
{
    private static Logger? _logger;

    public static Logger? Logger()
    {
        try
        {
            _logger ??= new Logger("DevSetupEngine", GetLoggingOptions());
        }
        catch
        {
            // Do nothing if logger fails.
        }

        return _logger;
    }

    public static Options GetLoggingOptions()
    {
        return new Options
        {
            LogFileFolderRoot = ApplicationData.Current.TemporaryFolder.Path,
            LogFileName = "DevSetupEngine_{now}.log",
            LogFileFolderName = "DevSetup",
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
            FailFastSeverity = FailFastSeverityLevel.Critical,
        };
    }
}
