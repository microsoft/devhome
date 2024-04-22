// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Utilities;

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
    }
}
