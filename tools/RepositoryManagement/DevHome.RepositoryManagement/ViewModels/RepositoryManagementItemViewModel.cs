// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    public const string EventName = "DevHome_RepositorySpecific_Event";

    public const string ErrorEventName = "DevHome_RepositorySpecificError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModel));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccess;

    private readonly StringResource _stringResource = new("DevHome.RepositoryManagement.pri", "DevHome.RepositoryManagement/Resources");

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly RepositoryEnhancerService _repositoryEnhancerService;

    private readonly IExtensionService _extensionService;

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
    /// <remarks>
    /// TODO: Test values are strings only.
    /// </remarks>
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
    private MenuFlyout _allSourceControlProviderNames;

    [RelayCommand]
    public void UpdateSourceControlProviderNames()
    {
        AllSourceControlProviderNames.Items.Clear();
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
    public async Task AssignRepositoryANewSourceControlProvider(IExtensionWrapper extensionWrapper)
    {
        if (!string.Equals(extensionWrapper.ExtensionClassId, SourceControlExtensionClassId, StringComparison.OrdinalIgnoreCase))
        {
            var result = await _repositoryEnhancerService.ReAssignSourceControl(ClonePath, extensionWrapper);
            if (result.Result != ResultType.Success)
            {
                ShowErrorContentDialog(result);
            }
            else
            {
                var repository = GetRepositoryReportIfNull(nameof(AssignRepositoryANewSourceControlProvider));
                _dataAccess.SetSourceControlId(repository, Guid.Parse(extensionWrapper.ExtensionClassId));
            }
        }
    }

    [RelayCommand]
    public async Task OpenInFileExplorer()
    {
        await CheckCloneLocationNotifyUserIfNotFound();
        OpenRepositoryInFileExplorer(RepositoryName, ClonePath, nameof(OpenInFileExplorer));
    }

    [RelayCommand]
    public async Task OpenInCMD()
    {
        await CheckCloneLocationNotifyUserIfNotFound();
        OpenRepositoryinCMD(RepositoryName, ClonePath, nameof(OpenInCMD));
    }

    [RelayCommand]
    public async Task MoveRepository()
    {
        // This action is not enabled due to a bug in FileExploreGitIntegration.
        // FileExplorerGitIntegration holds a lock on a file in this repository and it can not
        // be moved.
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

        var newDirectoryInfo = new DirectoryInfo(Path.Join(newLocation, RepositoryName));
        var currentDirectoryInfo = new DirectoryInfo(Path.GetFullPath(ClonePath));

        try
        {
            currentDirectoryInfo.MoveTo(newDirectoryInfo.FullName);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Cound not move repository to the selected location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(MoveRepository)));
        }

        var repository = GetRepositoryReportIfNull(nameof(MoveRepository));
        if (repository == null)
        {
            return;
        }

        var didUpdate = _dataAccess.UpdateCloneLocation(repository, newDirectoryInfo.FullName);

        if (!didUpdate)
        {
            _log.Error($"Could not update the database.  Check logs");
        }

        ClonePath = Path.Join(newLocation, RepositoryName);
    }

    [RelayCommand]
    public async Task DeleteRepositoryAsync()
    {
        // TODO: Add repository name and the location to the dialog.
        // TODO: Ask user to type in the repository name before removing.
        // This action is not enabled due to a bug in FileExploreGitIntegration.
        // FileExplorerGitIntegration holds a lock on a file in this repository and it can not
        // be moved.
        var cantFindRepositoryDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = $"Would you like to delete this repository?",
            Content = $"Deleting a repository means it will be permanently removed in File Explorer and from your PC.",
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
        };

        ContentDialogResult dialogResult = ContentDialogResult.None;

        try
        {
            dialogResult = await cantFindRepositoryDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to open confirmation dialog.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(DeleteRepositoryAsync)));
        }

        if (dialogResult != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            // Remove the repository.
            // TODO: Check if this location is a repository and the name matches the repo name
            // in path.
            if (!string.IsNullOrEmpty(ClonePath)
                && Directory.Exists(ClonePath))
            {
                // Cumbersome, but needed to remove read-only files.
                foreach (var repositoryFile in Directory.EnumerateFiles(ClonePath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(repositoryFile, FileAttributes.Normal);
                    File.Delete(repositoryFile);
                }

                foreach (var repositoryDirectory in Directory.GetDirectories(ClonePath, "*", SearchOption.AllDirectories).Reverse())
                {
                    Directory.Delete(repositoryDirectory);
                }

                File.SetAttributes(ClonePath, FileAttributes.Normal);
                Directory.Delete(ClonePath, false);
            }

            var repository = GetRepositoryReportIfNull(nameof(DeleteRepositoryAsync));
            if (repository == null)
            {
                // Do not warn the user here.  If the repository is not in the database
                // the repository management page will not display the repository
                // when entities are fetched.
                return;
            }

            _dataAccess.RemoveRepository(repository);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error when deleting the repository.");
            TelemetryFactory.Get<ITelemetry>().Log(
                "DevHome_RepositoryLineItem_Event",
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(DeleteRepositoryAsync)));
        }
    }

    [RelayCommand]
    public async Task MakeConfigurationFileWithThisRepository()
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
                var repository = GetRepositoryReportIfNull(nameof(MakeConfigurationFileWithThisRepository));
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
        string cloneLocation)
    {
        _window = window;
        _dataAccess = dataAccess;
        _configurationFileBuilder = configurationFileBuilder;
        RepositoryName = repositoryName;
        _clonePath = cloneLocation;
        _extensionService = extensionService;
        _allSourceControlProviderNames = new MenuFlyout();
        _repositoryEnhancerService = repositoryEnhancerService;
    }

    public void RemoveThisRepositoryFromTheList()
    {
        var repository = GetRepositoryReportIfNull(nameof(RemoveThisRepositoryFromTheList));
        if (repository == null)
        {
            return;
        }

        _dataAccess.SetIsHidden(repository, true);
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
            // TODO: read stdout/stderror for errors in execution.
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
        new RepositoryLineItemErrorEvent(operation, ex.HResult, ex.Message));

        _log.Error(ex, string.Empty);
    }

    private async Task ShowCloneLocationNotFoundDialogAsync()
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
            SendTelemetryAndLogError(nameof(ShowCloneLocationNotFoundDialogAsync), ex);
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

            var repository = GetRepositoryReportIfNull(nameof(ShowCloneLocationNotFoundDialogAsync));

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
            return;
        }
    }

    private Repository GetRepositoryReportIfNull(string action)
    {
        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
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

    private async Task CheckCloneLocationNotifyUserIfNotFound()
    {
        if (!Directory.Exists(Path.GetFullPath(ClonePath)))
        {
            // Ask the user if they can point DevHome to the correct location
            await ShowCloneLocationNotFoundDialogAsync();
        }
    }

    public async void ShowErrorContentDialog(SourceControlValidationResult result)
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
