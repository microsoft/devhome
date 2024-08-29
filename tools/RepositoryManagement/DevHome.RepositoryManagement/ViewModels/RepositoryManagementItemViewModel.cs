// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.RepositoryManagement;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.Windows.FileDialog;
using DevHome.RepositoryManagement.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementItemViewModel
{
    public const string EventName = "DevHome_RepositorySpecific_Event";

    public const string ErrorEventName = "DevHome_RepositorySpecificError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModel));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccess;

    public string RepositoryName { get; set; } = string.Empty;

    public string ClonePath { get; set; } = string.Empty;

    public string LatestCommit { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public bool IsHiddenFromPage { get; set; }

    [RelayCommand]
    public async Task OpenInFileExplorer()
    {
        var localRepositoryName = RepositoryName;
        if (string.IsNullOrEmpty(RepositoryName))
        {
            _log.Warning("RepositoryName is either null or empty.");
            localRepositoryName = string.Empty;
        }

        var localClonePath = ClonePath;
        if (string.IsNullOrEmpty(ClonePath))
        {
            _log.Warning("ClonePath is either null or empty");
            localClonePath = string.Empty;
        }

        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(localClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(localRepositoryName, ClonePath);
        }

        OpenRepositoryInFileExplorer(localRepositoryName, localClonePath, nameof(OpenInFileExplorer));
    }

    [RelayCommand]
    public async Task OpenInCMD()
    {
        var localRepositoryName = RepositoryName;
        if (string.IsNullOrEmpty(RepositoryName))
        {
            _log.Warning("RepositoryName is either null or empty.");
            localRepositoryName = string.Empty;
        }

        var localClonePath = ClonePath;
        if (string.IsNullOrEmpty(ClonePath))
        {
            _log.Warning("ClonePath is either null or empty");
            localClonePath = string.Empty;
        }

        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(localClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(localRepositoryName, ClonePath);
        }

        OpenRepositoryinCMD(localRepositoryName, localClonePath, nameof(OpenInCMD));
    }

    [RelayCommand]
    public void MoveRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void DeleteRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void MakeConfigurationFileWithThisRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void OpenFileExplorerToConfigurationsFolder()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void RemoveThisRepositoryFromTheList()
    {
        throw new NotImplementedException();
    }

    public RepositoryManagementItemViewModel(Window window, RepositoryManagementDataAccessService dataAccess)
    {
        _window = window;
        _dataAccess = dataAccess;
    }

    private void OpenRepositoryInFileExplorer(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in File Explorer at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        // Not catching PathTooLongException.  If the file was in a location that had a too long path,
        // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
        processStartInfo.FileName = Path.GetFullPath(cloneLocation);

        StartProcess(processStartInfo, action);
    }

    private void OpenRepositoryinCMD(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in CMD at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        // Not catching PathTooLongException.  If the file was in a location that had a too long path,
        // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
        processStartInfo.FileName = "CMD";
        processStartInfo.WorkingDirectory = Path.GetFullPath(cloneLocation);

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
        string repositoryName,
        string cloneLocation)
    {
        // strings need to be localized
        ContentDialog cantFindRepositoryDialog = new ContentDialog()
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

            var oldLocation = ClonePath;
            var repository = _dataAccess.GetRepository(RepositoryName, oldLocation);

            // The user clicked on this menu from the repository management page.
            // The repository should be in the database.
            // Somehow getting the repository returned null.
            if (repository is null)
            {
                _log.Warning($"The repository with name {RepositoryName} and clone location {oldLocation} is not in the database when it is expected to be there.");
                TelemetryFactory.Get<ITelemetry>().Log(
                    EventName,
                    LogLevel.Critical,
                    new RepositoryLineItemEvent(nameof(OpenInFileExplorer), repositoryName));

                return;
            }

            // The repository exists at the location stored in the Database
            // and the new location is set.
            var didUpdate = _dataAccess.UpdateCloneLocation(repository, newLocation);

            if (!didUpdate)
            {
                _log.Warning($"Could not update the database.  Check logs");
            }

            var didSave = _dataAccess.Save();
            if (!didSave)
            {
                _log.Warning($"Could not save to the database.  Check logs");
            }
        }
        else if (dialogResult == ContentDialogResult.Secondary)
        {
            RemoveThisRepositoryFromTheList();
            return;
        }
    }
}
