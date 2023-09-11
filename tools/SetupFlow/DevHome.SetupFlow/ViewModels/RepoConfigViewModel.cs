// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.TaskGroups;
using DevHome.SetupFlow.Utilities;
using Microsoft.Windows.DevHome.SDK;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// The view model to handle the whole repo tool.
/// </summary>
public partial class RepoConfigViewModel : SetupPageViewModelBase
{
    /// <summary>
    /// All the tasks that need to be ran during the loading page.
    /// </summary>
    private readonly RepoConfigTaskGroup _taskGroup;

    private readonly IDevDriveManager _devDriveManager;

    private readonly IThemeSelectorService _themeSelectorService;

    /// <summary>
    /// The minimum available space the user should have on the drive that holds their OS, in gigabytes.
    /// This value is not in bytes.
    /// </summary>
    private const double MinimumAvailableSpaceInGbForDevDriveAutoCheckbox = 200D;

    /// <summary>
    /// The minimum available space the user should have on the drive that holds their OS, in bytes.
    /// </summary>
    private readonly ulong _minimumAvailableSpaceInBytesForDevDriveAutoCheckbox = DevDriveUtil.ConvertToBytes(MinimumAvailableSpaceInGbForDevDriveAutoCheckbox, ByteUnit.GB);

    private bool _shouldAutoCheckDevDriveCheckbox = true;

    public ISetupFlowStringResource LocalStringResource { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the Dev Drive checkbox should be enabled when the user launches the add repository dialog.
    /// If the user unchecks the checkbox then we respect their choice for that instance of the setup flow.
    /// </summary>
    public bool ShouldAutoCheckDevDriveCheckbox
    {
        get
        {
            try
            {
                var osDrive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                if (!osDrive.IsReady || _minimumAvailableSpaceInBytesForDevDriveAutoCheckbox > (ulong)osDrive.AvailableFreeSpace)
                {
                    _shouldAutoCheckDevDriveCheckbox = false;
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Unable to check if Dev Drive checkbox should be auto checked: {ex.Message}");
                _shouldAutoCheckDevDriveCheckbox = false;
            }

            return _shouldAutoCheckDevDriveCheckbox;
        }

        set => _shouldAutoCheckDevDriveCheckbox = value;
    }

    /// <summary>
    /// All repositories the user wants to clone.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<CloningInformation> _repoReviewItems = new ();

    public IDevDriveManager DevDriveManager => _devDriveManager;

    public RepoConfigViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IDevDriveManager devDriveManager,
        RepoConfigTaskGroup taskGroup,
        IHost host)
        : base(stringResource, orchestrator)
    {
        _taskGroup = taskGroup;
        _devDriveManager = devDriveManager;
        LocalStringResource = stringResource;
        RepoDialogCancelled += _devDriveManager.CancelChangesToDevDrive;
        PageTitle = StringResource.GetLocalized(StringResourceKey.ReposConfigPageTitle);
        NextPageButtonToolTipText = stringResource.GetLocalized(StringResourceKey.RepoToolNextButtonTooltip);
        _themeSelectorService = host.GetService<IThemeSelectorService>();
        _themeSelectorService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object sender, ElementTheme e)
    {
        ElementTheme themeToSwitchTo = e;

        if (themeToSwitchTo == ElementTheme.Default)
        {
            if (_themeSelectorService.IsDarkTheme())
            {
                themeToSwitchTo = ElementTheme.Dark;
            }
            else
            {
                themeToSwitchTo = ElementTheme.Light;
            }
        }

        // Because the logos aren't glyphs DevHome has to change the logos manually to match the theme.
        foreach (var cloneInformation in RepoReviewItems)
        {
            cloneInformation.SetIcon(themeToSwitchTo);
        }
    }

    /// <summary>
    /// Saves all cloning informations to be cloned during the loading screen.
    /// </summary>
    /// <param name="cloningInformations">All repositories the user selected.</param>
    /// <remarks>
    /// Makes a new collection to force UI to update.
    /// </remarks>
    public void SaveSetupTaskInformation(List<CloningInformation> cloningInformations)
    {
        // Handle the case where a user re-opens the repo tool with repos that are already selected
        // Remove them from cloninginformations so they aren't added again.
        var alreadyAddedRepos = RepoReviewItems.Intersect(cloningInformations).ToList();

        var localCloningInfos = new List<CloningInformation>(cloningInformations);
        foreach (var alreadyAddedRepo in alreadyAddedRepos)
        {
            localCloningInfos.Remove(alreadyAddedRepo);
        }

        foreach (var cloningInformation in localCloningInfos)
        {
            RepoReviewItems.Add(cloningInformation);
        }

        // RemoveCloningInformation calls save.  If we don't call RemoveCloningInformation repo tool
        // should call save.
        var shouldCallSave = true;

        // Handle the case that a user de-selected a repo from re-opening the repo tool.
        // This is the case where RepoReviewItems does not contain a repo in cloningInformations.
        var deSelectedRepos = RepoReviewItems.Except(cloningInformations).ToList();
        foreach (var deSelectedRepo in deSelectedRepos)
        {
            // Ignore repos added via URL.  They would get removed here.
            if (deSelectedRepo.OwningAccount != null)
            {
                RemoveCloningInformation(deSelectedRepo);
                shouldCallSave = false;
            }
        }

        if (shouldCallSave)
        {
            RepoReviewItems = new ObservableCollection<CloningInformation>(RepoReviewItems);
            _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
        }
    }

    /// <summary>
    /// Remove a specific cloning location from the list of repos to clone.
    /// </summary>
    /// <param name="cloningInformation">The cloning information to remove.</param>
    public void RemoveCloningInformation(CloningInformation cloningInformation)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, $"Removing repository {cloningInformation.RepositoryId} from repos to clone");
        RepoReviewItems.Remove(cloningInformation);

        // force collection to be empty(?) converter won't fire otherwise.
        if (RepoReviewItems.Count == 0)
        {
            RepoReviewItems = new ObservableCollection<CloningInformation>();
        }

        _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
    }

    public void UpdateCloneLocation(CloningInformation cloningInformation)
    {
        var location = RepoReviewItems.IndexOf(cloningInformation);
        if (location != -1)
        {
            RepoReviewItems[location] = cloningInformation;
            _taskGroup.SaveSetupTaskInformation(RepoReviewItems.ToList());
        }
    }

    /// <summary>
    /// Update the collection of items that are being cloned to the Dev Drive. With new information
    /// should the user choose to change the information with the customize button.
    /// </summary>
    /// <param name="cloningInfo">Cloning info that has a new path for the Dev Drive</param>
    public void UpdateCollectionWithDevDriveInfo(CloningInformation cloningInfo)
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Updating dev drive location on repos to clone after change to dev drive");
        foreach (var item in RepoReviewItems)
        {
            if (item.CloneToDevDrive && item.CloningLocation != cloningInfo.CloningLocation)
            {
                Log.Logger?.ReportDebug(Log.Component.RepoConfig, $"Updating {item.RepositoryId}");
                item.CloningLocation = new System.IO.DirectoryInfo(cloningInfo.CloningLocation.FullName);
                item.CloneLocationAlias = cloningInfo.CloneLocationAlias;
            }
        }
    }

    /// <summary>
    /// Event that the Dev Drive manager can subscribe to, to know when and if the Add repo or edit clone path
    /// dialogs closed using the cancel button.
    /// </summary>
    /// <remarks>
    /// This will send back the original Dev Drive object back to the Dev Drive manager who will update its
    /// list. This is because clicking the save button in the Dev Drive window will overwrite the Dev Drive
    /// information. However, the user can still select cancel from one of the repo dialogs. Selecting cancel
    /// there should revert the changes made to the Dev Drive object the manager hold.
    /// </remarks>
    public event Action RepoDialogCancelled = () => { };

    public void ReportDialogCancellation()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Repo add/edit dialog cancelled");
        RepoDialogCancelled();
    }
}
