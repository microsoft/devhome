// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

public sealed class ElevatedInstallTaskProgress
{
    public ElevatedInstallTaskProgress(int state, double downloadProgress, double installationProgress)
    {
        State = state;
        DownloadProgress = downloadProgress;
        InstallationProgress = installationProgress;
    }

    internal ElevatedInstallTaskProgress(WinGetInstallPackageProgress progress)
        : this((int)progress.State, progress.DownloadProgress, progress.InstallationProgress)
    {
    }

    public int State { get; }

    public double DownloadProgress { get; }

    public double InstallationProgress { get; }
}
