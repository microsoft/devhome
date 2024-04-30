// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Utilities;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.UI.Notifications;

namespace DevHome.Customization.ViewModels.DevDriveInsights;

/// <summary>
/// View model for the card that represents a dev drive on the dev drive insights page.
/// </summary>
public partial class DevDriveCardViewModel : ObservableObject
{
    public string DevDriveLabel { get; set; }

    public ulong DevDriveSize { get; set; }

    public ulong DevDriveFreeSize { get; set; }

    public ulong DevDriveUsedSize { get; set; }

    public double DevDriveFillPercentage { get; set; }

    public string DevDriveUnitOfMeasure { get; set; }

    public string DevDriveSizeText { get; set; }

    public string DevDriveUsedSizeText { get; set; }

    public string DevDriveFreeSizeText { get; set; }

    public bool IsDevDriveTrusted { get; set; }

    public string DevDriveTrustText { get; set; }

    public char DriveLetter { get; set; }

    [RelayCommand]
    private void MakeDevDriveTrusted()
    {
        // Launch a UAC prompt
        var startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = Environment.SystemDirectory + "\\fsutil.exe";

        // Run the fstutil cmd to trust the dev drive
        startInfo.Arguments = "devdrv trust /f " + DriveLetter + ":";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;

        // Since a UAC prompt will be shown, we need to wait for the process to exit
        // This can also be cancelled by the user which will result in an exception
        try
        {
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    public DevDriveCardViewModel(IDevDrive devDrive)
    {
        DevDriveLabel = devDrive.DriveLabel;
        var divider = (ulong)((devDrive.DriveUnitOfMeasure == ByteUnit.TB) ? 1000_000_000_000 : 1000_000_000);
        DevDriveSize = devDrive.DriveSizeInBytes / divider;
        DevDriveFreeSize = devDrive.DriveSizeRemainingInBytes / divider;
        DevDriveUsedSize = DevDriveSize - DevDriveFreeSize;
        DevDriveUnitOfMeasure = (devDrive.DriveUnitOfMeasure == ByteUnit.TB) ? "TB" : "GB";
        DevDriveFillPercentage = ((DevDriveSize - DevDriveFreeSize) * 100) / DevDriveSize;
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        DevDriveSizeText = stringResource.GetLocalized("DevDriveSizeText", DevDriveSize, DevDriveUnitOfMeasure);
        DevDriveUsedSizeText = stringResource.GetLocalized("DevDriveUsedSizeText", DevDriveUsedSize, DevDriveUnitOfMeasure);
        DevDriveFreeSizeText = stringResource.GetLocalized("DevDriveFreeSizeText", DevDriveFreeSize, DevDriveUnitOfMeasure);
        IsDevDriveTrusted = devDrive.IsDevDriveTrusted;
        DevDriveTrustText = IsDevDriveTrusted ? stringResource.GetLocalized("DevDriveTrustedText") : stringResource.GetLocalized("DevDriveUntrustedText");
        DriveLetter = devDrive.DriveLetter;
    }
}
