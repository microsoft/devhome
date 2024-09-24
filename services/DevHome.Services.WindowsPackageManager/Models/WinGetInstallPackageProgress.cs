// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Models;

public sealed class WinGetInstallPackageProgress
{
    public WinGetInstallPackageProgress(
        WinGetInstallPackageProgressState state,
        ulong bytesDownloaded,
        ulong bytesRequired,
        double downloadProgress,
        double installationProgress)
    {
        State = state;
        BytesDownloaded = bytesDownloaded;
        BytesRequired = bytesRequired;
        DownloadProgress = downloadProgress;
        InstallationProgress = installationProgress;
    }

    internal WinGetInstallPackageProgress(InstallProgress progress)
    {
        State = (WinGetInstallPackageProgressState)progress.State;
        BytesDownloaded = progress.BytesDownloaded;
        BytesRequired = progress.BytesRequired;
        DownloadProgress = progress.DownloadProgress;
        InstallationProgress = progress.InstallationProgress;
    }

    public WinGetInstallPackageProgressState State { get; }

    public ulong BytesDownloaded { get; }

    public ulong BytesRequired { get; }

    public double DownloadProgress { get; }

    public double InstallationProgress { get; }
}
