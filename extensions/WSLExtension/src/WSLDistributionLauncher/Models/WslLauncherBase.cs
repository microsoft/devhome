// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Win32.Foundation;

namespace WSLDistributionLauncher.Models;

internal abstract class WslLauncherBase
{
    public BOOL UseCurrentWorkingDirectory { get; } = false;

    public string DistributionName { get; init; } = string.Empty;

    public string Commands { get; init; } = string.Empty;

    public abstract void Launch();
}
