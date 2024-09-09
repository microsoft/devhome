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
using DevHome.Database.Services;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

// TODO: Clean up the code.
public partial class RepositoryManagementItemViewModel : ObservableObject
{
    public const string RepoNamePrefix = "Clone ";

    public const string RepoNameSuffix = ": ";

    public const string EventName = "DevHome_RepositorySpecific_Event";

    public const string ErrorEventName = "DevHome_RepositorySpecificError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModel));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccess;

    private readonly IStringResource _stringResource;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private string _repositoryName;

    /// <summary>
    /// Gets or sets the name of the repository.  Nulls are converted to string.empty.
    /// </summary>
    public string RepositoryName
    {
        get => _repositoryName ?? string.Empty;

        set => _repositoryName = value ?? string.Empty;
    }

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

    [RelayCommand]
    public async Task OpenInFileExplorer()
    {
        var localClonePath = ClonePath ?? string.Empty;

        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(localClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(RepositoryName);
        }

        OpenRepositoryInFileExplorer(RepositoryName, localClonePath, nameof(OpenInFileExplorer));
    }

    [RelayCommand]
    public async Task OpenInCMD()
    {
        var localClonePath = ClonePath ?? string.Empty;

        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(localClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(RepositoryName);
        }

        OpenRepositoryinCMD(RepositoryName, localClonePath, nameof(OpenInCMD));
    }

    [RelayCommand]
    public async Task MoveRepository()
    {
        // TODO: Save to the database before moving the folder.
        var newLocation = await PickNewLocationForRepositoryAsync();

        // TODO: Warn the user no action will take place
        if (string.IsNullOrEmpty(newLocation))
        {
            _log.Information("The path from the folder picker is either null or empty.  Not updating the clone path");
            return;
        }

        if (string.Equals(Path.GetFullPath(newLocation), Path.GetFullPath(ClonePath), StringComparison.OrdinalIgnoreCase))
        {
            _log.Information("The selected path is the same as the current path.");
            return;
        }

        var localOldClonePath = ClonePath ?? string.Empty;
        var repository = _dataAccess.GetRepository(RepositoryName, localOldClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {localOldClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(OpenInFileExplorer), RepositoryName));

            return;
        }

        var newDirectoryInfo = new DirectoryInfo(Path.Join(newLocation, RepositoryName));
        var currentDirectoryInfo = new DirectoryInfo(Path.GetFullPath(localOldClonePath));

        try
        {
            currentDirectoryInfo.MoveTo(newDirectoryInfo.FullName);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Cound not move repository to the selected location.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(MoveRepository), RepositoryName));
        }

        // The repository exists at the location stored in the Database
        // and the new location is set.
        var didUpdate = _dataAccess.UpdateCloneLocation(repository, newDirectoryInfo.FullName);

        if (!didUpdate)
        {
            _log.Warning($"Could not update the database.  Check logs");
        }

        ClonePath = Path.Join(newLocation, RepositoryName);
    }

    [RelayCommand]
    public async Task DeleteRepository()
    {
        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(DeleteRepository), RepositoryName));

            return;
        }

        var cantFindRepositoryDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = $"Would you like to delete this repository?",
            Content = $"Deleting a repository means it will be permanently removed in File Explorer and from your PC.",
            PrimaryButtonText = $"Yes",
            CloseButtonText = "Cancel",
        };

        var dialogResult = await cantFindRepositoryDialog.ShowAsync();

        if (dialogResult == ContentDialogResult.Primary)
        {
            // Remove the repository.
            // TODO: Check if this location is a repository and the name matches the repo name
            // in path.
            if (!string.IsNullOrEmpty(ClonePath)
                && Directory.Exists(ClonePath))
            {
                // Cumbersome, but needed to remove read-only files.
                foreach (var myFile in Directory.EnumerateFiles(ClonePath, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(myFile, FileAttributes.Normal);
                    File.Delete(myFile);
                }

                foreach (var myDirectory in Directory.GetDirectories(ClonePath, "*", SearchOption.AllDirectories).Reverse())
                {
                    Directory.Delete(myDirectory);
                }

                File.SetAttributes(ClonePath, FileAttributes.Normal);
                Directory.Delete(ClonePath, false);

                _dataAccess.RemoveRepository(repository);
            }
        }
    }

    [RelayCommand]
    public async Task MakeConfigurationFileWithThisRepository()
    {
        try
        {
            // Show the save file dialog
            using var fileDialog = new WindowSaveFileDialog();

            // TODO: Needs Localization
            fileDialog.AddFileType(_stringResource.GetLocalized("{0} file", "YAML"), ".winget");
            fileDialog.AddFileType(_stringResource.GetLocalized("{0} file", "YAML"), ".dsc.yaml");
            var fileName = fileDialog.Show(_window);

            // If the user selected a file, write the configuration to it
            if (!string.IsNullOrEmpty(fileName))
            {
                var repositoryToUse = _dataAccess.GetRepository(RepositoryName, ClonePath);
                var configFile = _configurationFileBuilder.GetConfigurationFileForRepoAndGit(repositoryToUse);
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
        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(OpenInFileExplorer), RepositoryName));

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

    [RelayCommand]
    public void RemoveThisRepositoryFromTheList()
    {
        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(OpenInFileExplorer), RepositoryName));

            return;
        }

        _dataAccess.SetIsHidden(repository, true);
    }

    public RepositoryManagementItemViewModel(
        Window window,
        RepositoryManagementDataAccessService dataAccess,
        IStringResource stringResource,
        ConfigurationFileBuilder configurationFileBuilder)
    {
        _window = window;
        _dataAccess = dataAccess;
        _stringResource = stringResource;
        _configurationFileBuilder = configurationFileBuilder;
    }

    private void OpenRepositoryInFileExplorer(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in File Explorer at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

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
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

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
        ErrorEventName,
        LogLevel.Critical,
        new RepositoryLineItemErrorEvent(operation, ex.HResult, ex.Message, RepositoryName));

        _log.Error(ex, string.Empty);
    }

    private async Task CloneLocationNotFoundNotifyUser(
        string repositoryName)
    {
        // strings need to be localized
        var cantFindRepositoryDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = $"Can not find {RepositoryName}.",
            Content = $"Cannot find {RepositoryName} at {Path.GetFullPath(ClonePath)}.  Do you know where it is?",
            PrimaryButtonText = $"Locate {RepositoryName} via File Explorer.",
            SecondaryButtonText = "Remove from list",
            CloseButtonText = "Cancel",
        };

        var dialogResult = await cantFindRepositoryDialog.ShowAsync();

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

            var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

            // The user clicked on this menu from the repository management page.
            // The repository should be in the database.
            // Somehow getting the repository returned null.
            if (repository is null)
            {
                _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
                TelemetryFactory.Get<ITelemetry>().Log(
                    EventName,
                    LogLevel.Critical,
                    new RepositoryLineItemEvent(nameof(OpenInFileExplorer), repositoryName));

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

    /*
    public WinGetConfigFile DownloadConfigFileFromARepository(Repository repository)
    {
        List<WinGetConfigResource> resources = [];
        resources.Add(MakeSomethingFromARepository(repository));
        resources.Add(CreateWinGetInstallForGitPreReq());

        var wingetConfigProperties = new WinGetConfigProperties();

        // Merge the resources into the Resources property in the properties object
        wingetConfigProperties.Resources = resources.ToArray();
        wingetConfigProperties.ConfigurationVersion = DscHelpers.WinGetConfigureVersion;

        // Create the new WinGetConfigFile object and serialize it to yaml
        return new WinGetConfigFile() { Properties = wingetConfigProperties };
    }

    private WinGetConfigResource MakeSomethingFromARepository(Repository repository)
    {
        // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
        // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
        var id = $"{RepoNamePrefix}{repository.RepositoryName}{RepoNameSuffix}{Path.GetFullPath(repository.RepositoryClonePath)}";

        var gitDependsOnId = DscHelpers.GitWinGetPackageId;

        // TODO: Add clone URL to the database
        return new WinGetConfigResource()
        {
            Resource = DscHelpers.GitCloneDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Cloning: {repository.RepositoryName}" },
            DependsOn = [gitDependsOnId],
            Settings = new GitDscSettings() { HttpsUrl = string.Empty, RootDirectory = Path.GetFullPath(repository.RepositoryClonePath) },
        };
    }

    private WinGetConfigResource CreateWinGetInstallForGitPreReq()
    {
        var id = DscHelpers.GitWinGetPackageId;

        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Installing {DscHelpers.GitName}" },
            Settings = new WinGetDscSettings() { Id = DscHelpers.GitWinGetPackageId, Source = DscHelpers.DscSourceNameForWinGet },
        };
    }
    */
}
