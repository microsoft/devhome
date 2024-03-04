// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.TelemetryEvents.SetupFlow.RepoTool;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

/// <summary>
/// Shows the user the repositories they have selected.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class RepoConfigView : UserControl
{
    private Guid ActivityId => ViewModel.Orchestrator.ActivityId;

    public RepoConfigViewModel ViewModel => (RepoConfigViewModel)this.DataContext;

    private AddRepoDialog _addRepoDialog;

    public RepoConfigView()
    {
        this.InitializeComponent();
        ActualThemeChanged += OnActualThemeChanged;
    }

    public void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (ViewModel != null)
        {
            // Because the logos aren't glyphs DevHome has to change the logos manually to match the theme.
            foreach (var cloneInformation in ViewModel.RepoReviewItems)
            {
                cloneInformation.SetIcon(sender.ActualTheme);
            }
        }

        if (_addRepoDialog != null)
        {
            _addRepoDialog.RequestedTheme = sender.ActualTheme;
        }
    }

    /// <summary>
    /// User wants to add a repo.  Bring up the tool.
    /// </summary>
    [RelayCommand]
    private async Task AddRepoAsync()
    {
        // hold information for telemetry calls
        const string EventName = "RepoTool_AddRepos_Event";
        var dialogName = "RepoDialog";
        var telemetryLogger = TelemetryFactory.Get<ITelemetry>();

        telemetryLogger.Log(EventName, LogLevel.Critical, new DialogEvent("Open", dialogName), ActivityId);

        _addRepoDialog = new AddRepoDialog(ViewModel.DevDriveManager, ViewModel.LocalStringResource, ViewModel.RepoReviewItems.ToList(), ActivityId, ViewModel.Host);
        var getExtensionsTask = _addRepoDialog.GetExtensionsAsync();
        var setupDevDrivesTask = _addRepoDialog.AddRepoViewModel.SetupDevDrivesAsync();
        _addRepoDialog.XamlRoot = RepoConfigGrid.XamlRoot;
        _addRepoDialog.RequestedTheme = ActualTheme;

        // Start
        await getExtensionsTask;
        await setupDevDrivesTask;

        _addRepoDialog.SetDeveloperIdChangedEvents();

        if (_addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.CanShowDevDriveUI && ViewModel.ShouldAutoCheckDevDriveCheckbox)
        {
            _addRepoDialog.UpdateDevDriveInfo();
        }

        _addRepoDialog.IsSecondaryButtonEnabled = true;
        var result = await _addRepoDialog.ShowAsync(ContentDialogPlacement.InPlace);

        var devDrive = _addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.DevDrive;

        if (_addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.IsWindowOpen)
        {
            ViewModel.DevDriveManager.RequestToCloseDevDriveWindow(devDrive);
        }

        // save cloneLocationKind for telemetry
        CloneLocationKind cloneLocationKind = CloneLocationKind.LocalPath;
        var everythingToClone = _addRepoDialog.AddRepoViewModel.EverythingToClone;

        foreach (var repoToClone in everythingToClone)
        {
            repoToClone.SetIcon(ActualTheme);
        }

        // Handle the case the user de-selected all repos.
        if (result == ContentDialogResult.Primary && everythingToClone.Count == 0)
        {
            ViewModel.SaveSetupTaskInformation(everythingToClone);
        }

        if (result == ContentDialogResult.Primary && everythingToClone.Count != 0)
        {
            // Currently clone path supports either a local path or a new Dev Drive. Only one can be selected
            // during the add repo dialog flow. If multiple repositories are selected and the user chose to clone them to a Dev Drive
            // that doesn't exist on the system yet, then we make sure all the locations will clone to that new Dev Drive.
            if (devDrive != null && devDrive.State != DevDriveState.ExistsOnSystem)
            {
                foreach (var cloneInfo in everythingToClone)
                {
                    cloneInfo.CloneToDevDrive = true;
                    cloneInfo.CloneLocationAlias = _addRepoDialog.AddRepoViewModel.FolderPickerViewModel.CloneLocationAlias;
                }

                // The cloning location may have changed e.g The original Drive clone path for Dev Drives was the F: drive for items
                // on the add repo page, but during the Add repo dialog flow the user chose to change this location to the D: drive.
                // reflect this for all the old items currently in the add repo page.
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

            // Check if user unchecked the Dev Drive checkbox before closing, to update the the behavior the next time the user launches the dialog. Note we only keep
            // track of this for the current launch of the setup flow. If the user completes or cancels the setup flow and re enters, we do not keep the unchecked behavior.
            if (!_addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.IsDevDriveCheckboxChecked)
            {
                ViewModel.ShouldAutoCheckDevDriveCheckbox = false;
            }
        }
        else
        {
            // User cancelled the dialog, Report back to the Dev drive Manager to revert any changes.
            ViewModel.ReportDialogCancellation();
        }

        // Convert current page to addkind.  Currently users can add either by URL or account (via the repos page)
        AddKind addKind = AddKind.URL;
        if (_addRepoDialog.AddRepoViewModel.CurrentPage == Models.Common.PageKind.Repositories)
        {
            addKind = AddKind.Account;
        }

        // Only 1 provider can be selected per repo dialog session.
        // Okay to use EverythingToClone[0].ProviderName here.
        var providerName = _addRepoDialog.AddRepoViewModel.EverythingToClone.Count != 0 ? _addRepoDialog.AddRepoViewModel.EverythingToClone[0].ProviderName : string.Empty;

        // If needs be, this can run inside a foreach loop to capture details on each repo.
        if (cloneLocationKind == CloneLocationKind.DevDrive)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                "RepoDialog_RepoAdded_Event",
                LogLevel.Critical,
                RepoDialogAddRepoEvent.AddWithDevDrive(
                addKind,
                _addRepoDialog.AddRepoViewModel.EverythingToClone.Count,
                providerName,
                _addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.DevDrive.State == DevDriveState.New,
                _addRepoDialog.AddRepoViewModel.EditDevDriveViewModel.DevDriveDetailsChanged),
                ActivityId);
        }
        else if (cloneLocationKind == CloneLocationKind.LocalPath)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                "RepoDialog_RepoAdded_Event",
                LogLevel.Critical,
                RepoDialogAddRepoEvent.AddWithLocalPath(
                addKind,
                _addRepoDialog.AddRepoViewModel.EverythingToClone.Count,
                providerName),
                ActivityId);
        }

        telemetryLogger.Log(EventName, LogLevel.Critical, new DialogEvent("Close", dialogName, result), ActivityId);
    }

    /// <summary>
    /// User wants to edit the clone location of a repo.  Show the dialog.
    /// </summary>
    /// <param name="sender">Used to find the cloning information clicked on.</param>
    private async void EditClonePathButton_Click(object sender, RoutedEventArgs e)
    {
        const string EventName = "RepoTool_EditClonePath_Event";
        var dialogName = "EditClonePath";
        var telemetryLogger = TelemetryFactory.Get<ITelemetry>();

        telemetryLogger.Log(EventName, LogLevel.Critical, new DialogEvent("Open", dialogName), ActivityId);

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
                telemetryLogger.Log(EventName, LogLevel.Critical, new SwitchedCloningLocationEvent(CloneLocationKind.LocalPath, devDrive?.State == DevDriveState.New, devDrive?.State == DevDriveState.ExistsOnSystem), ActivityId);
                ViewModel.DevDriveManager.DecreaseRepositoriesCount();
                ViewModel.DevDriveManager.CancelChangesToDevDrive();
            }

            if (cloningInformation.CloneToDevDrive)
            {
                ViewModel.DevDriveManager.ConfirmChangesToDevDrive();

                // User switched from local path to Dev Drive
                if (!wasCloningToDevDrive)
                {
                    telemetryLogger.Log(EventName, LogLevel.Critical, new SwitchedCloningLocationEvent(CloneLocationKind.DevDrive), ActivityId);
                    ViewModel.DevDriveManager.IncreaseRepositoriesCount(1);
                }

                cloningInformation.CloneLocationAlias = editClonePathDialog.FolderPickerViewModel.CloneLocationAlias;
                ViewModel.UpdateCloneLocation(cloningInformation);
            }

            // If the user launches the edit button, and changes or updates the clone path to be a Dev Drive,
            // update the other entries in the list, that are being cloned to the Dev Drive with this new information.
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

        telemetryLogger.Log(EventName, LogLevel.Critical, new DialogEvent("Close", dialogName, result), ActivityId);
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

        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_RepoList_Event", LogLevel.Critical, new RepoConfigEvent("Remove"), ActivityId);
    }
}
