// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.RepositoryManagement;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Helpers;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Services;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementItemViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModel));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccess;

    private readonly StringResource _stringResource = new("DevHome.RepositoryManagement.pri", "DevHome.RepositoryManagement/Resources");

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly RepositoryEnhancerService _repositoryEnhancerService;

    private readonly IExtensionService _extensionService;

    private readonly Action _updateCallback;

    /// <summary>
    /// Gets the name of the repository.
    /// </summary>
    public string RepositoryName { get; }

    [ObservableProperty]
    private string _clonePath;

    private string _latestCommit;

    /// <summary>
    /// Gets or sets the latest commit.  Nulls are converted to string.empty.
    /// </summary>
    public string LatestCommit
    {
        get => _latestCommit ?? string.Empty;

        set => _latestCommit = value ?? string.Empty;
    }

    private string _branch;

    /// <summary>
    /// Gets or sets the local branch name.  Nulls are converted to string.empty.
    /// </summary>
    public string Branch
    {
        get => _branch ?? string.Empty;
        set => _branch = value ?? string.Empty;
    }

    public bool IsHiddenFromPage { get; set; }

    public bool HasAConfigurationFile { get; set; }

    public string LatestCommitSHA { get; set; }

    public string LatestCommitAuthor { get; set; }

    public string MinutesSinceLatestCommit { get; set; }

    public bool HasCommitInformation { get; set; }

    public string MoreOptionsButtonAutomationName { get; set; }

    [ObservableProperty]
    private string _sourceControlProviderDisplayName;

    [ObservableProperty]
    private string _sourceControlProviderPackageDisplayName;

    public string SourceControlExtensionClassId { get; set; }

    [ObservableProperty]
    private bool _enableAllOperationsExceptRunConfiguartion;

    [ObservableProperty]
    private bool _shouldShowMovingRepositoryProgressRing;

    [ObservableProperty]
    private MenuFlyout _allSourceControlProviderNames;

    [RelayCommand]
    public void UpdateSourceControlProviderNames()
    {
        // Making a new MenuFlyout in the constructor throws an empty exception because
        // the flyout is made in a non-UI thread.  Move the constructor here instead.
        AllSourceControlProviderNames = new MenuFlyout();
        foreach (var extension in _extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result.ToList())
        {
            var menuItem = new MenuFlyoutItem
            {
                Text = extension.ExtensionDisplayName,
                Tag = extension,
            };

            menuItem.Command = AssignRepositoryANewSourceControlProviderCommand;
            menuItem.CommandParameter = extension;

            ToolTipService.SetToolTip(menuItem, _stringResource.GetLocalized("PrefixForDevHomeVersion", extension.PackageDisplayName));
            AllSourceControlProviderNames.Items.Add(menuItem);
        }
    }

    [RelayCommand]
    public async Task AssignRepositoryANewSourceControlProviderAsync(IExtensionWrapper extensionWrapper)
    {
        if (!string.Equals(extensionWrapper.ExtensionClassId, SourceControlExtensionClassId, StringComparison.OrdinalIgnoreCase))
        {
            var result = await _repositoryEnhancerService.ReAssignSourceControl(ClonePath, extensionWrapper);
            if (result.Result != ResultType.Success)
            {
                ShowErrorContentDialogAsync(result);
            }
            else
            {
                var repository = GetRepositoryReportIfNull(nameof(AssignRepositoryANewSourceControlProviderAsync));
                _dataAccess.SetSourceControlId(repository, Guid.Parse(extensionWrapper.ExtensionClassId));
            }
        }
    }

    [RelayCommand]
    public async Task OpenInFileExplorerAsync()
    {
        await CheckCloneLocationNotifyUserIfNotFoundAsync();
        OpenRepositoryInFileExplorer(RepositoryName, ClonePath, nameof(OpenInFileExplorerAsync));
    }

    [RelayCommand]
    public async Task OpenInCMDAsync()
    {
        await CheckCloneLocationNotifyUserIfNotFoundAsync();
        OpenRepositoryinCMD(RepositoryName, ClonePath, nameof(OpenInCMDAsync));
    }

    [RelayCommand]
    public async Task MoveRepositoryAsync()
    {
        var newLocation = await PickNewLocationForRepositoryAsync();

        if (string.IsNullOrEmpty(newLocation))
        {
            _log.Information("The path from the folder picker is either null or empty.  Not updating the clone path");
            return;
        }

        if (string.Equals(Path.GetFullPath(newLocation), Path.GetFullPath(ClonePath), StringComparison.OrdinalIgnoreCase))
        {
            _log.Information("The selected path is the same as the current path.  Not updating the clone path");
            return;
        }

        var newClonePath = Path.Join(newLocation, RepositoryName);

        try
        {
            EnableAllOperationsExceptRunConfiguartion = false;
            ShouldShowMovingRepositoryProgressRing = true;

            await Task.Run(() =>
            {
                // Store all file system entry attributes to restore after the move.
                // FileSystem.MoveDirectory removes read-only attribute of files and folders.
                Dictionary<string, FileAttributes> attributes = new();
                foreach (var repositoryFile in Directory.GetFileSystemEntries(ClonePath, "*", SearchOption.AllDirectories))
                {
                    var theFullPath = Path.GetFullPath(repositoryFile);
                    theFullPath = theFullPath.Replace(ClonePath, newClonePath);
                    attributes.Add(Path.GetFullPath(theFullPath), File.GetAttributes(repositoryFile));
                }

                // Directory.Move does not move across drives.
                Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(ClonePath, newClonePath);

                foreach (var newFile in Directory.GetFileSystemEntries(newClonePath, "*", SearchOption.AllDirectories))
                {
                    var fullPathToEntry = Path.GetFullPath(newFile);
                    File.SetAttributes(fullPathToEntry, attributes[Path.GetFullPath(fullPathToEntry)]);
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Cound not move repository to the selected location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(MoveRepositoryAsync)));
        }

        var repository = GetRepositoryReportIfNull(nameof(MoveRepositoryAsync));
        if (repository == null)
        {
            return;
        }

        var didUpdate = _dataAccess.UpdateCloneLocation(repository, newClonePath);

        if (!didUpdate)
        {
            _log.Error($"Could not update the database.  Check logs");
        }

        _repositoryEnhancerService.RemoveTrackedRepository(ClonePath);
        await _repositoryEnhancerService.MakeRepositoryEnhanced(newClonePath, _repositoryEnhancerService.GetSourceControlProvider(SourceControlExtensionClassId));

        try
        {
            RepositoryActionHelper.DeleteEverything(ClonePath);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Could not remove all of {RepositoryName} from {ClonePath}.");
        }

        ClonePath = newClonePath;

        ShouldShowMovingRepositoryProgressRing = false;
        EnableAllOperationsExceptRunConfiguartion = true;
    }

    [RelayCommand]
    public void HideRepository()
    {
        RemoveThisRepositoryFromTheList();
        _updateCallback();
    }

    [RelayCommand]
    public async Task MakeConfigurationFileWithThisRepositoryAsync()
    {
        try
        {
            // Show the save file dialog
            using var fileDialog = new WindowSaveFileDialog();
            fileDialog.AddFileType(_stringResource.GetLocalized("ConfigurationFileNameFormat", "YAML"), ".winget");
            fileDialog.AddFileType(_stringResource.GetLocalized("ConfigurationFileNameFormat", "YAML"), ".dsc.yaml");
            var fileName = fileDialog.Show(_window);

            // If the user selected a file, write the configuration to it
            if (!string.IsNullOrEmpty(fileName))
            {
                var repositoryToUse = _dataAccess.GetRepository(RepositoryName, ClonePath);
                var repository = GetRepositoryReportIfNull(nameof(MakeConfigurationFileWithThisRepositoryAsync));
                if (repository == null)
                {
                    return;
                }

                var configFile = _configurationFileBuilder.MakeConfigurationFileForRepoAndGit(repositoryToUse);
                await File.WriteAllTextAsync(fileName, configFile);
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to download configuration file.");
        }
    }

    [RelayCommand]
    public void RunConfigurationFile()
    {
        var repository = GetRepositoryReportIfNull(nameof(RunConfigurationFile));
        if (repository == null)
        {
            return;
        }

        if (!repository.HasAConfigurationFile)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} does not have a configuration file.");
            return;
        }

        var configurationFileLocation = repository.ConfigurationFileLocation ?? string.Empty;
        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = "winget";
        processStartInfo.ArgumentList.Add("configure");
        processStartInfo.ArgumentList.Add(configurationFileLocation);
        processStartInfo.Verb = "RunAs";

        StartProcess(processStartInfo, nameof(RunConfigurationFile));
    }

    internal RepositoryManagementItemViewModel(
        Window window,
        RepositoryManagementDataAccessService dataAccess,
        ConfigurationFileBuilder configurationFileBuilder,
        IExtensionService extensionService,
        RepositoryEnhancerService repositoryEnhancerService,
        string repositoryName,
        string cloneLocation,
        Action updateCallback)
    {
        _window = window;
        _dataAccess = dataAccess;
        _configurationFileBuilder = configurationFileBuilder;
        RepositoryName = repositoryName;
        _clonePath = cloneLocation;
        _extensionService = extensionService;
        _repositoryEnhancerService = repositoryEnhancerService;
        _updateCallback = updateCallback;
        _enableAllOperationsExceptRunConfiguartion = true;
    }

    public void RemoveThisRepositoryFromTheList()
    {
        var repository = GetRepositoryReportIfNull(nameof(RemoveThisRepositoryFromTheList));
        if (repository == null)
        {
            return;
        }

        _dataAccess.SetIsHidden(repository, true);
        IsHiddenFromPage = true;
    }

    private void OpenRepositoryInFileExplorer(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in File Explorer at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            "DevHome_RepositoryLineItem_Event",
            LogLevel.Critical,
            new RepositoryLineItemEvent(action));

        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,

            // Not catching PathTooLongException.  If the file was in a location that had a too long path,
            // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
            FileName = Path.GetFullPath(cloneLocation),
        };

        StartProcess(processStartInfo, action);
    }

    private void OpenRepositoryinCMD(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in CMD at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            "DevHome_RepositoryLineItem_Event",
            LogLevel.Critical,
            new RepositoryLineItemEvent(action));

        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,

            // Not catching PathTooLongException.  If the file was in a location that had a too long path,
            // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
            FileName = "CMD",
            WorkingDirectory = Path.GetFullPath(cloneLocation),
        };

        StartProcess(processStartInfo, action);
    }

    private void StartProcess(ProcessStartInfo processStartInfo, string operation)
    {
        try
        {
            Process.Start(processStartInfo);
        }
        catch (Exception e)
        {
            SendTelemetryAndLogError(operation, e);
        }
    }

    /// <summary>
    /// Opens the directory picker
    /// </summary>
    /// <returns>A string that is the full path the user chose.</returns>
    private async Task<string> PickNewLocationForRepositoryAsync()
    {
        try
        {
            _log.Information("Opening folder picker to select a new location");
            using var folderPicker = new WindowOpenFolderDialog();
            var newLocation = await folderPicker.ShowAsync(_window);
            if (newLocation != null && newLocation.Path.Length > 0)
            {
                _log.Information($"Selected '{newLocation.Path}' for the repository path.");
                return Path.GetFullPath(newLocation.Path);
            }
            else
            {
                _log.Information("Didn't select a location to clone to");
                return null;
            }
        }
        catch (Exception e)
        {
            _log.Error(e, "Failed to open folder picker");
            return null;
        }
    }

    private void SendTelemetryAndLogError(string operation, Exception ex)
    {
        TelemetryFactory.Get<ITelemetry>().LogError(
        "DevHome_RepositoryLineItemError_Event",
        LogLevel.Critical,
        new RepositoryLineItemErrorEvent(operation, ex));

        _log.Error(ex, string.Empty);
    }

    private async Task CloneLocationNotFoundNotifyUserAsync()
    {
        // strings need to be localized
        var cantFindRepositoryDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = _stringResource.GetLocalized("LocateRepositoryDialogTitle", RepositoryName),
            Content = _stringResource.GetLocalized("LocateRepositoryDialogContent", RepositoryName, ClonePath),
            PrimaryButtonText = _stringResource.GetLocalized("LocateRepositoryDialogFindWithFileExplorer"),
            SecondaryButtonText = _stringResource.GetLocalized("LocateRepositoryRemoveFromListInstead"),
            CloseButtonText = _stringResource.GetLocalized("Cancel"),
        };

        // https://github.com/microsoft/microsoft-ui-xaml/issues/424
        // Setting MaxWidth does not change the dialog size.
        cantFindRepositoryDialog.Resources["ContentDialogMaxWidth"] = 700;

        ContentDialogResult dialogResult = ContentDialogResult.None;

        try
        {
            dialogResult = await cantFindRepositoryDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            SendTelemetryAndLogError(nameof(CloneLocationNotFoundNotifyUserAsync), ex);
        }

        // User will show DevHome where the repository is.
        // Open the folder picker.
        // Maybe don't close the dialog until the user is done
        // with the folder picker.
        if (dialogResult == ContentDialogResult.Primary)
        {
            var newLocation = await PickNewLocationForRepositoryAsync();
            if (string.IsNullOrEmpty(newLocation))
            {
                _log.Information("The path from the folder picker is either null or empty.  Not updating the clone path");
                return;
            }

            var repository = GetRepositoryReportIfNull(nameof(CloneLocationNotFoundNotifyUserAsync));

            if (repository == null)
            {
                return;
            }

            // The repository exists at the location stored in the Database
            // and the new location is set.
            var didUpdate = _dataAccess.UpdateCloneLocation(repository, Path.Combine(newLocation, RepositoryName));

            if (!didUpdate)
            {
                _log.Warning($"Could not update the database.  Check logs");
            }
        }
        else if (dialogResult == ContentDialogResult.Secondary)
        {
            RemoveThisRepositoryFromTheList();
            _updateCallback();
            return;
        }
    }

    private Repository GetRepositoryReportIfNull(string action)
    {
        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemEvent(action));

            return null;
        }

        return repository;
    }

    private async Task CheckCloneLocationNotifyUserIfNotFoundAsync()
    {
        if (!Directory.Exists(Path.GetFullPath(ClonePath)))
        {
            // Ask the user if they can point DevHome to the correct location
            await CloneLocationNotFoundNotifyUserAsync();
        }
    }

    public async void ShowErrorContentDialogAsync(SourceControlValidationResult result)
    {
        var errorDialog = new ContentDialog
        {
            Title = _stringResource.GetLocalized("ErrorAssigningSourceControlProvider"),
            Content = result.DisplayMessage,
            CloseButtonText = _stringResource.GetLocalized("CloseButtonText"),
            XamlRoot = _window.Content.XamlRoot,
        };
        _ = await errorDialog.ShowAsync();
    }
}
