// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.TelemetryEvents.RepositoryManagement;
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

    private readonly XamlRoot _root;

    public string RepositoryName { get; set; } = string.Empty;

    public string ClonePath { get; set; } = string.Empty;

    public string LatestCommit { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public bool IsHiddenFromPage { get; set; }

    [RelayCommand]
    public async Task OpenInFileExplorer()
    {
        if (string.IsNullOrEmpty(RepositoryName))
        {
            _log.Warning("RepositoryName is either null or empty.");
        }

        if (string.IsNullOrEmpty(ClonePath))
        {
            _log.Warning("ClonePath is either null or empty");
        }
        else if (!Directory.Exists(Path.GetFullPath(ClonePath)))
        {
            ContentDialog noWifiDialog = new ContentDialog()
            {
                XamlRoot = _root,
                Title = $"Can not find {RepositoryName}.",
                Content = $"Cannot find {RepositoryName} at {Path.GetFullPath(ClonePath)}.  Do you know where it is?",
                PrimaryButtonText = $"Locate {RepositoryName}",
                SecondaryButtonText = "Remove from list",
                CloseButtonText = "Cancel",
            };

            await noWifiDialog.ShowAsync();
        }

        // use string.empty to prevent null refrence exceptions.
        var localRepositoryName = string.IsNullOrEmpty(RepositoryName) ? string.Empty : RepositoryName;
        var localClonePath = string.IsNullOrEmpty(ClonePath) ? string.Empty : ClonePath;

        _log.Information($"Showing {localRepositoryName} in File Explorer at location {localClonePath}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(nameof(OpenInFileExplorer), localRepositoryName));

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        // Not catching PathTooLongException.  If the file was in a location that had a too long path,
        // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
        processStartInfo.FileName = Path.GetFullPath(localClonePath);

        StartProcess(processStartInfo, nameof(OpenInFileExplorer));
    }

    [RelayCommand]
    public void OpenInCMD()
    {
        throw new NotImplementedException();
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

    public RepositoryManagementItemViewModel(Window window)
    {
        _root = window.Content.XamlRoot;
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

    private void SendTelemetryAndLogError(string operation, Exception ex)
    {
        TelemetryFactory.Get<ITelemetry>().LogError(
        ErrorEventName,
        LogLevel.Critical,
        new RepositoryLineItemErrorEvent(operation, ex.HResult, ex.Message, RepositoryName));

        _log.Error(ex, string.Empty);
    }
}
