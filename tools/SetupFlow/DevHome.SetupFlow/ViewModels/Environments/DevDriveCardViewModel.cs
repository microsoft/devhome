// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Utilities;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

using Dispatching = Microsoft.UI.Dispatching;

namespace DevHome.SetupFlow.ViewModels.Environments;

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

    private const int _maxCardProperties = 6;

    public DevDrive DevDriveWrapper { get; private set; }

    public BitmapImage DevDriveImage { get; set; }

    public BitmapImage DevDriveProviderImage { get; set; }

    public string DevDriveProviderName { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _devDriveTitle;

    [ObservableProperty]
    private string _devDriveProviderDisplayName;

    [ObservableProperty]
    private DevDriveState _cardState;

    [ObservableProperty]
    private CardStateColor _stateColor;

    // This will be used for the accessibility name of the dev drive card.
    [ObservableProperty]
    private Lazy<string> _accessibilityName;

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
    }
}
