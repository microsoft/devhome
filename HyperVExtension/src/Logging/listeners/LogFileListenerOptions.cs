// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Logging;

public partial class Options
{
    private const string LogFileNameDefault = "DevHomeHyperVExtension.log";
    private const string LogFileFolderNameDefault = "{now}";

    public string LogFileName { get; set; } = LogFileNameDefault;

    public string LogFileFolderName { get; set; } = LogFileFolderNameDefault;

    // The Temp Path is used for storage by default so tests can run this code without being packaged.
    // If we directly put in the ApplicationData folder, it would fail anytime the program was not packaged.
    // For use with packaged application, set in Options to:
    //     ApplicationData.Current.TemporaryFolder.Path
    public string LogFileFolderRoot { get; set; } = Path.GetTempPath();

    public string LogFileFolderPath => Path.Combine(LogFileFolderRoot, LogFileFolderName);

    public bool LogFileEnabled { get; set; } = true;

    public SeverityLevel LogFileFilter { get; set; } = SeverityLevel.Info;
}
