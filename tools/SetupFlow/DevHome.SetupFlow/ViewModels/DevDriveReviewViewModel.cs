// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.ViewModels;

public partial class DevDriveReviewViewModel : ReviewTabViewModelBase
{
    private readonly IHost _host;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly DevDriveTaskGroup _devDriveTaskGroup;

    public DevDriveReviewViewModel(IHost host, ISetupFlowStringResource stringResource, DevDriveTaskGroup devDriveTaskGroup)
    {
        _host = host;
        _stringResource = stringResource;
        TabTitle = stringResource.GetLocalized(StringResourceKey.DevDriveReviewTitle);
        _devDriveTaskGroup = devDriveTaskGroup;
    }

    public override bool HasItems => Application.Current.GetService<IDevDriveManager>().DevDrivesMarkedForCreation.Any();

    /// <summary>
    /// Gets the a collection of <see cref="DevDriveReviewTabItem"/> to be displayed on the Basics review tab in the
    /// setup flow review page.
    /// </summary>
    public ObservableCollection<DevDriveReviewTabItem> DevDrives
    {
        get
        {
            ObservableCollection<DevDriveReviewTabItem> devDriveReviewTabItem = new();
            var manager = Application.Current.GetService<IDevDriveManager>();
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
