// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.DevDrive.ViewModels;

public partial class DevDriveReviewViewModel : ReviewTabViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly DevDriveTaskGroup _taskGroup;
    private readonly List<IDevDrive> _devDrives;
    private readonly string _localizedCountOfDevDrives;
    private readonly string _numberOfItemsTitle;

    public DevDriveReviewViewModel(IHost host, ILogger logger, ISetupFlowStringResource stringResource, DevDriveTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        _taskGroup = taskGroup;
        TabTitle = stringResource.GetLocalized(StringResourceKey.Basics);
        _devDrives = new (host.GetService<IDevDriveManager>().DevDrivesMarkedForCreation);
        _numberOfItemsTitle = stringResource.GetLocalized(StringResourceKey.DevDriveReviewPageNumberOfDevDrivesTitle);
        _localizedCountOfDevDrives = _stringResource.GetLocalized(StringResourceKey.DevDriveReviewPageNumberOfDevDrives, _devDrives.Count);
    }

    /// <summary>
    /// Gets the a collection of  <see cref="DevDriveReviewTabItem"/> to be displayed on the Basics review tab in the
    /// setup flow review page.
    /// </summary>
    public ObservableCollection<DevDriveReviewTabItem> DevDrives
    {
        get
        {
            ObservableCollection<DevDriveReviewTabItem> devDriveReviewTabItem = new ();
            foreach (var devDrive in _devDrives)
            {
                devDriveReviewTabItem.Add(new DevDriveReviewTabItem(devDrive));
            }

            return devDriveReviewTabItem;
        }
    }

    public string LocalizedCountOfDevices => _localizedCountOfDevDrives;

    public string NumberOfItemsTitle => _numberOfItemsTitle;
}
