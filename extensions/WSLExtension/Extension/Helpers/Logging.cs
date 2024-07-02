// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Storage;

namespace WSLExtension.Helpers;

public static class Logging
{
    public static readonly string LogFolderName = "Logs";

    public static readonly string WslSubFolderName = "WSL";

    private static readonly Lazy<string> _logFolderRoot = new(() => Path.Combine(ApplicationData.Current.TemporaryFolder.Path, LogFolderName));

    public static readonly string RootDevHomeLogFolder = _logFolderRoot.Value;

    public static readonly string PathToWslLogFolder = Path.Combine(RootDevHomeLogFolder, WslSubFolderName);
}
