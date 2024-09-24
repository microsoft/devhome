// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.WindowsPackageManager.Models;

public enum WinGetInstallPackageProgressState
{
    Queued = unchecked((int)0),
    Downloading = unchecked((int)0x1),
    Installing = unchecked((int)0x2),
    PostInstall = unchecked((int)0x3),
    Finished = unchecked((int)0x4),
}
