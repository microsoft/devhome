// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveReviewViewModel : ReviewTabViewModelBase
{
    private readonly IHost _host;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IDevDriveManager _devDriveManager;

    public DevDriveReviewViewModel(IHost host, ISetupFlowStringResource stringResource, IDevDriveManager devDriveManager)
    {
        _host = host;
        _stringResource = stringResource;
        _devDriveManager = devDriveManager;
        TabTitle = stringResource.GetLocalized(StringResourceKey.DevDriveReviewTitle);
    }

    public override bool HasItems => _devDriveManager.DevDrivesMarkedForCreation.Any();

    /// <summary>
    /// Gets the a collection of <see cref="DevDriveReviewTabItem"/> to be displayed on the Basics review tab in the
    /// setup flow review page.
    /// </summary>
    public ObservableCollection<DevDriveReviewTabItem> DevDrives
    {
        get
        {
            ObservableCollection<DevDriveReviewTabItem> devDriveReviewTabItem = new ();
            if (_devDriveManager.RepositoriesUsingDevDrive > 0)
            {
                foreach (var devDrive in _devDriveManager.DevDrivesMarkedForCreation)
                {
                    devDriveReviewTabItem.Add(new DevDriveReviewTabItem(devDrive));
                }
            }

            return devDriveReviewTabItem;
        }
    }
}
