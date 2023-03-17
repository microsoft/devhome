// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.DevDrive.Models;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace DevHome.SetupFlow.DevDrive.ViewModels;

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
    /// In the form of "Dev Disk 1 (D:). We attempt to localize the file size, and fallback to english if that fails.
    /// </summary>
    /// <param name="devDrive">The Dev drive object to get the data from</param>
    public DevDriveReviewTabItem(IDevDrive devDrive)
    {
        unsafe
        {
            var buffer = new string(' ', (int)PInvoke.MAX_PATH);
            fixed (char* tempPath = buffer)
            {
                var result =
                    PInvoke.StrFormatByteSizeEx(
                        devDrive.DriveSizeInBytes,
                        SFBS_FLAGS.SFBS_FLAGS_TRUNCATE_UNDISPLAYED_DECIMAL_DIGITS,
                        tempPath,
                        PInvoke.MAX_PATH);
                if (result != 0)
                {
                    // fallback to using community toolkit which shows this unlocalized. In the form of 50 GB, 40 TB etc.
                    _size = Converters.ToFileSizeString((long)devDrive.DriveSizeInBytes);
                }
                else
                {
                    _size = new string(buffer).Trim();
                }
            }
        }

        _formattedLabelAndDriveLetter = $"{devDrive.DriveLabel} ({devDrive.DriveLetter}:)";
        _location = devDrive.DriveLocation;
    }
}
