// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

public class Logging
{
    public static readonly string LogExtension = ".dhlog";

    public static readonly string LogFolderName = "Logs";

    public static readonly string AppName = "DevSetupAgent";

    public static readonly string DefaultLogFileName = "hyperv_setup";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(Path.GetTempPath(), AppName, LogFolderName));

    public static readonly string LogFolderRoot = _logFolderRoot.Value;
}
