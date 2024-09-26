// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Represents the state of the installation progress.
/// </summary>
/// <remarks>
/// Reference: https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl
/// </remarks>
public enum WinGetInstallPackageProgressState
{
    Queued = unchecked((int)0),
    Downloading = unchecked((int)0x1),
    Installing = unchecked((int)0x2),
    PostInstall = unchecked((int)0x3),
    Finished = unchecked((int)0x4),
}
