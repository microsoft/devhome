// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Represents the progress of a Windows Package Manager installation
/// operation.
/// </summary>
public sealed class WinGetInstallPackageProgress
{
    public WinGetInstallPackageProgress(
        WinGetInstallPackageProgressState state,
        double downloadProgress,
        double installationProgress)
    {
        State = state;
        DownloadProgress = downloadProgress;
        InstallationProgress = installationProgress;
    }

    internal WinGetInstallPackageProgress(InstallProgress progress)
    {
        State = (WinGetInstallPackageProgressState)progress.State;
        DownloadProgress = progress.DownloadProgress;
        InstallationProgress = progress.InstallationProgress;
    }

    /// <summary>
    /// Gets the current state of the installation operation.
    /// </summary>
    public WinGetInstallPackageProgressState State { get; }

    /// <summary>
    /// Gets the download percentage complete.
    /// </summary>
    public double DownloadProgress { get; }

    /// <summary>
    /// Gets the installation percentage complete.
    /// </summary>
    public double InstallationProgress { get; }
}
