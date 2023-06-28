// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Windows.Security.Authentication.Identity.Provider;
using static DevHome.SetupFlow.Models.Common;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Dialog to allow users to select repositories they want to clone.
/// </summary>
internal partial class AddRepoDialog
{
    private readonly string _defaultClonePath;

    private readonly List<CloningInformation> _previouslySelectedRepos = new ();

    /// <summary>
    /// Gets or sets the view model to handle selecting and de-selecting repositories.
    /// </summary>
    public AddRepoViewModel AddRepoViewModel
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the view model to handle adding a dev drive.
    /// </summary>
    public EditDevDriveViewModel EditDevDriveViewModel
    {
        get; set;
    }

    /*
    /// <summary>
    /// Hold the clone location in case the user decides not to add a dev drive.
    /// </summary>
    private string _oldCloneLocation;
    */

    public AddRepoDialog(IDevDriveManager devDriveManager, ISetupFlowStringResource stringResource, List<CloningInformation> previouslySelectedRepos)
    {
        this.InitializeComponent();

        _previouslySelectedRepos = previouslySelectedRepos;
        AddRepoViewModel = new AddRepoViewModel(stringResource, previouslySelectedRepos);
        EditDevDriveViewModel = new EditDevDriveViewModel(devDriveManager);

        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _defaultClonePath = Path.Join(userFolder, "source", "repos");
        AddRepoViewModel.CloneLocation = _defaultClonePath;

        EditDevDriveViewModel.DevDriveClonePathUpdated += (_, updatedDevDriveRootPath) =>
        {
            AddRepoViewModel.CloneLocationAlias = EditDevDriveViewModel.GetDriveDisplayName(DevDriveDisplayNameKind.FormattedDriveLabelKind);
            AddRepoViewModel.CloneLocation = updatedDevDriveRootPath;
        };

        /*
        // Changing view to account so the selection changed event for Segment correctly shows URL.
        AddRepoViewModel.CurrentPage = PageKind.AddViaAccount;
        IsPrimaryButtonEnabled = false;
        AddViaUrlSegmentedItem.IsSelected = true;
        SwitchViewsSegmentedView.SelectedIndex = 1;
        */
    }
}
