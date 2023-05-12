// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Shows the user the repositories they have sleected.
/// </summary>
public sealed partial class RepoConfigView : UserControl
{
    private readonly Guid relatedActivityId;

    public RepoConfigView()
    {
        relatedActivityId = Guid.NewGuid();
        this.InitializeComponent();
    }

    public RepoConfigViewModel ViewModel => (RepoConfigViewModel)this.DataContext;

    /// <summary>
    /// User wants to add a repo.  Bring up the tool.
    /// </summary>
    private async void AddRepoButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // hold information for telemetry calls
        const string EventName = "RepoTool_AddRepos_Event";
        var dialogName = "RepoDialog";
        var telemetryLogger = TelemetryFactory.Get<ITelemetry>();

        telemetryLogger.Log(EventName, LogLevel.Measure, new DialogEvent("Open", dialogName), relatedActivityId);

        // Both the hyperlink button and button call this.
        // disable the button to prevent users from double clicking it.
        var senderAsButton = sender as Button;
        if (senderAsButton != null)
        {
            senderAsButton.IsEnabled = false;
        }

        var addRepoDialog = new AddRepoDialog(ViewModel.DevDriveManager, ViewModel.LocalStringResource, ViewModel.RepoReviewItems.ToList());
        var getPluginsTask = addRepoDialog.GetPluginsAsync();
        var setupDevDrivesTask = addRepoDialog.SetupDevDrivesAsync();
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        addRepoDialog.XamlRoot = RepoConfigGrid.XamlRoot;
        addRepoDialog.RequestedTheme = themeService.Theme;

        // Start
        await getPluginsTask;
        await setupDevDrivesTask;
        var result = await addRepoDialog.ShowAsync(ContentDialogPlacement.InPlace);

        if (senderAsButton != null)
        {
            senderAsButton.IsEnabled = true;
        }

        var devDrive = addRepoDialog.EditDevDriveViewModel.DevDrive;

        if (addRepoDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(devDrive);
        }

        // save cloneLocationKind for telemetry
        CloneLocationKind cloneLocationKind = CloneLocationKind.LocalPath;
        var everythingToClone = addRepoDialog.AddRepoViewModel.EverythingToClone;
        if (result == ContentDialogResult.Primary && everythingToClone.Any())
        {
            // We currently only support adding either a local path or a new Dev Drive as the cloning location. Only one can be selected
            // during the add repo dialog flow. So if multiple repositories are selected and the user chose to clone them to a Dev Drive
            // that doesn't exist on the system yet, then we make sure all the locations will clone to that new Dev Drive.
            if (devDrive != null && devDrive.State != DevDriveState.ExistsOnSystem)
            {
                foreach (var cloneInfo in everythingToClone)
                {
                    cloneInfo.CloneToDevDrive = true;
                    cloneInfo.CloneLocationAlias = addRepoDialog.FolderPickerViewModel.CloneLocationAlias;
                }

                // The cloning location may have changed e.g The original Drive clone path for Dev Drives was the F: drive for items
                // on the add repo page, but during the Add repo dialog flow the user chose to change this location to the D: drive.
                // we need to reflect this for all the old items currently in the add repo page.
                ViewModel.UpdateCollectionWithDevDriveInfo(everythingToClone.First());
                ViewModel.DevDriveManager.IncreaseRepositoriesCount(everythingToClone.Count);
                ViewModel.DevDriveManager.ConfirmChangesToDevDrive();
            }

            if (devDrive != null)
            {
                cloneLocationKind = CloneLocationKind.DevDrive;
                foreach (var cloneInfo in everythingToClone)
                {
                    cloneInfo.CloneToExistingDevDrive = devDrive.State == DevDriveState.ExistsOnSystem;
                }
            }

            // Two states to worry about
            // 1. Adding repos that haven't been selected, and
            // 2. Removing repos from the pre selected list.
            ViewModel.SaveSetupTaskInformation(everythingToClone);
        }
        else
        {
            // User cancelled the dialog, Report back to the Dev drive Manager to revert any changes.
            ViewModel.ReportDialogCancellation();
        }

        // Convert current page to addkind.  Currently users can add either by URL or account (via the repos page)
        AddKind addKind = AddKind.URL;
        if (addRepoDialog.AddRepoViewModel.CurrentPage == Models.Common.PageKind.Repositories)
        {
            addKind = AddKind.Account;
        }

        // Only 1 provider can be selected per repo dialog session.
        // Okay to use EverythingToClone[0].ProviderName here.
        var providerName = addRepoDialog.AddRepoViewModel.EverythingToClone.Any() ? addRepoDialog.AddRepoViewModel.EverythingToClone[0].ProviderName : string.Empty;

        // If needs be, this can run inside a foreach loop to capture details on each repo.
        if (cloneLocationKind == CloneLocationKind.DevDrive)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                "RepoDialog_RepoAdded_Event",
                LogLevel.Measure,
                RepoDialogAddRepoEvent.AddWithDevDrive(
                addKind,
                addRepoDialog.AddRepoViewModel.EverythingToClone.Count,
                providerName,
                addRepoDialog.EditDevDriveViewModel.DevDrive.State == DevDriveState.New,
                addRepoDialog.EditDevDriveViewModel.DevDriveDetailsChanged),
                relatedActivityId);
        }
        else if (cloneLocationKind == CloneLocationKind.LocalPath)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                "RepoDialog_RepoAdded_Event",
                LogLevel.Measure,
                RepoDialogAddRepoEvent.AddWithLocalPath(
                addKind,
                addRepoDialog.AddRepoViewModel.EverythingToClone.Count,
                providerName),
                relatedActivityId);
        }

        telemetryLogger.Log(EventName, LogLevel.Measure, new DialogEvent("Close", dialogName, result), relatedActivityId);
    }

    /// <summary>
    /// User wants to edit the clone location of a repo.  Show the dialog.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private async void EditClonePathButton_Click(object sender, RoutedEventArgs e)
    {
        const string EventName = "RepoTool_EditClonePath_Event";
        var dialogName = "EditClonePath";
        var relatedActivityId = Guid.NewGuid();
        var telemetryLogger = TelemetryFactory.Get<ITelemetry>();

        telemetryLogger.Log(EventName, LogLevel.Measure, new DialogEvent("Open", dialogName), relatedActivityId);

        var cloningInformation = (sender as Button).DataContext as CloningInformation;
        var oldLocation = cloningInformation.CloningLocation;
        var wasCloningToDevDrive = cloningInformation.CloneToDevDrive;
        var editClonePathDialog = new EditClonePathDialog(ViewModel.DevDriveManager, cloningInformation, ViewModel.LocalStringResource);
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        editClonePathDialog.XamlRoot = RepoConfigGrid.XamlRoot;
        editClonePathDialog.RequestedTheme = themeService.Theme;
        var result = await editClonePathDialog.ShowAsync(ContentDialogPlacement.InPlace);

        var devDrive = editClonePathDialog.EditDevDriveViewModel.DevDrive;
        cloningInformation.CloneToDevDrive = devDrive != null;

        if (result == ContentDialogResult.Primary)
        {
            cloningInformation.CloningLocation = new System.IO.DirectoryInfo(editClonePathDialog.FolderPickerViewModel.CloneLocation);
            ViewModel.UpdateCloneLocation(cloningInformation);

            // User intended to clone to Dev Drive before launching dialog but now they are not,
            // so decrease the Dev Managers count.
            if (wasCloningToDevDrive && !cloningInformation.CloneToDevDrive)
            {
                telemetryLogger.Log(EventName, LogLevel.Measure, new SwitchedCloningLocationEvent(CloneLocationKind.LocalPath, devDrive.State == DevDriveState.New, devDrive.State == DevDriveState.ExistsOnSystem), relatedActivityId);
                ViewModel.DevDriveManager.DecreaseRepositoriesCount();
                ViewModel.DevDriveManager.CancelChangesToDevDrive();
            }

            if (cloningInformation.CloneToDevDrive)
            {
                ViewModel.DevDriveManager.ConfirmChangesToDevDrive();

                // User switched from local path to Dev Drive
                if (!wasCloningToDevDrive)
                {
                    telemetryLogger.Log(EventName, LogLevel.Measure, new SwitchedCloningLocationEvent(CloneLocationKind.DevDrive), relatedActivityId);
                    ViewModel.DevDriveManager.IncreaseRepositoriesCount(1);
                }

                cloningInformation.CloneLocationAlias = editClonePathDialog.FolderPickerViewModel.CloneLocationAlias;
                ViewModel.UpdateCloneLocation(cloningInformation);
            }

            // If the user launches the edit button, and changes or updates the clone path to be a Dev Drive, we need
            // to update the other entries in the list, that are being cloned to the Dev Drive with this new information.
            if (oldLocation != cloningInformation.CloningLocation && cloningInformation.CloneToDevDrive)
            {
                ViewModel.UpdateCollectionWithDevDriveInfo(cloningInformation);
            }
        }
        else
        {
            // User cancelled the dialog, Report back to the Dev drive Manager to revert any changes.
            ViewModel.ReportDialogCancellation();
            cloningInformation.CloneToDevDrive = wasCloningToDevDrive;
        }

        if (editClonePathDialog.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(editClonePathDialog.EditDevDriveViewModel.DevDrive);
        }

        telemetryLogger.Log(EventName, LogLevel.Measure, new DialogEvent("Close", dialogName, result), relatedActivityId);
    }

    /// <summary>
    /// Removes a repository to clone from the list.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private void RemoveCloningInformationButton_Click(object sender, RoutedEventArgs e)
    {
        var cloningInformation = (sender as Button).DataContext as CloningInformation;
        ViewModel.RemoveCloningInformation(cloningInformation);
        if (cloningInformation.CloneToDevDrive)
        {
            ViewModel.DevDriveManager.DecreaseRepositoriesCount();
        }

        TelemetryFactory.Get<ITelemetry>().LogMeasure("RepoTool_RemoveRepo_Event");
    }
}
