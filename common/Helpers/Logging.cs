// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Windows.Storage;

namespace DevHome.Common;

public class Logging
{
    public static readonly string LogExtension = ".dhlog";

    public static readonly string LogFolderName = "Logs";

    public static readonly string DefaultLogFileName = "devhome";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(ApplicationData.Current.TemporaryFolder.Path, LogFolderName));

    public static readonly string LogFolderRoot = _logFolderRoot.Value;

    public static void SetupLogging(string jsonFileName, string appName)
    {
        Environment.SetEnvironmentVariable("DEVHOME_LOGS_ROOT", Path.Join(LogFolderRoot, appName));
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(jsonFileName)
            .Build();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}
