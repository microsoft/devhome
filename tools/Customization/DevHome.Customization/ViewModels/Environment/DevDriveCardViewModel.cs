// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Utilities;

using Dispatching = Microsoft.UI.Dispatching;

namespace DevHome.Customization.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a dev drive on the setup target page.
/// </summary>
public partial class DevDriveCardViewModel : ObservableObject
{
    private readonly Dispatching.DispatcherQueue _dispatcher;

    private readonly DevHome.Common.Services.IDevDriveManager _devDriveManager;

    public string DevDriveLabel { get; set; }

    public ulong DevDriveSize { get; set; }

    public ulong DevDriveFreeSize { get; set; }

    public ulong DevDriveUsedSize { get; set; }

    public double DevDriveFillPercentage { get; set; }

    public string DevDriveUnitOfMeasure { get; set; }

    public string DevDriveSizeText { get; set; }

    public string DevDriveUsedSizeText { get; set; }

    public string DevDriveFreeSizeText { get; set; }

    public DevDriveCardViewModel(IDevDrive devDrive, IDevDriveManager manager)
    {
        _dispatcher = Dispatching.DispatcherQueue.GetForCurrentThread();
        _devDriveManager = manager;

        DevDriveLabel = devDrive.DriveLabel;
        var divider = (ulong)((devDrive.DriveUnitOfMeasure == ByteUnit.TB) ? 1000000000000 : 1000000000);
        DevDriveSize = devDrive.DriveSizeInBytes / divider;
        DevDriveFreeSize = devDrive.DriveSizeRemainingInBytes / divider;
        DevDriveUsedSize = DevDriveSize - DevDriveFreeSize;
        DevDriveUnitOfMeasure = (devDrive.DriveUnitOfMeasure == ByteUnit.TB) ? "TB" : "GB";
        DevDriveFillPercentage = ((DevDriveSize - DevDriveFreeSize) * 100) / DevDriveSize;
        var stringResource = new StringResource("DevHome.Customization/Resources");
        DevDriveSizeText = stringResource.GetLocalized("DevDriveSizeText", DevDriveSize, DevDriveUnitOfMeasure);
        DevDriveUsedSizeText = stringResource.GetLocalized("DevDriveUsedSizeText", DevDriveUsedSize, DevDriveUnitOfMeasure);
        DevDriveFreeSizeText = stringResource.GetLocalized("DevDriveFreeSizeText", DevDriveFreeSize, DevDriveUnitOfMeasure);
    }
}
