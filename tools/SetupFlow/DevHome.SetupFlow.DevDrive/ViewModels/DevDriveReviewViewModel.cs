// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly IHost _host;
    private readonly ISetupFlowStringResource _stringResource;

    public DevDriveReviewViewModel(IHost host, ILogger logger, ISetupFlowStringResource stringResource, DevDriveTaskGroup taskGroup)
    {
        _logger = logger;
        _stringResource = stringResource;
        TabTitle = stringResource.GetLocalized(StringResourceKey.DevDriveReviewTitle);
        _host = host;
    }

    /// <summary>
    /// Gets the a collection of <see cref="DevDriveReviewTabItem"/> to be displayed on the Basics review tab in the
    /// setup flow review page.
    /// </summary>
    public ObservableCollection<DevDriveReviewTabItem> DevDrives
    {
        get
        {
            var manager = _host.GetService<IDevDriveManager>();
            ObservableCollection<DevDriveReviewTabItem> devDriveReviewTabItem = new ();
            if (manager.RepositoriesUsingDevDrive > 0)
            {
                foreach (var devDrive in manager.DevDrivesMarkedForCreation)
                {
                    devDriveReviewTabItem.Add(new DevDriveReviewTabItem(devDrive));
                }
            }

            return devDriveReviewTabItem;
        }
    }
}
