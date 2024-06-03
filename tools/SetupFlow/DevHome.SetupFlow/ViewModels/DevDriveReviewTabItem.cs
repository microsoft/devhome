// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.SetupFlow.Utilities;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveReviewTabItem : ObservableObject
{
    private readonly string _formattedLabelAndDriveLetter;
    private readonly string _size;
    private readonly string _location;

    public string FormattedLabelAndDriveLetter => _formattedLabelAndDriveLetter;

    public string Size => _size;

    public string Location => _location;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevDriveReviewTabItem"/> class.
    /// We display the Drive label which is the same as the vhdx file name and display the drive letter.
    /// In the form of "Dev Disk 1 (D:).
    /// </summary>
    /// <param name="devDrive">The Dev drive object to get the data from</param>
    public DevDriveReviewTabItem(IDevDrive devDrive)
    {
        _size = DevDriveUtil.ConvertBytesToString(devDrive.DriveSizeInBytes);
        _formattedLabelAndDriveLetter = $"{devDrive.DriveLabel} ({devDrive.DriveLetter}:)";
        _location = devDrive.DriveLocation;
    }
}
