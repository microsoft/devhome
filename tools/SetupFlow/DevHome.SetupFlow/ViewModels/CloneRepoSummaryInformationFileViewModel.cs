// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Serilog;

namespace DevHome.SetupFlow.ViewModels;

public partial class CloneRepoSummaryInformationViewModel : ISummaryInformationViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(CloneRepoSummaryInformationViewModel));

    private const string _eventName = "CloneRepo_NextSteps_Event";

    private const string _runOperation = "run";

    private const string _viewOperation = "view";

    private readonly Guid _relatedActivityId;

    private readonly ISetupFlowStringResource _stringResource;

    public string FileName => Path.GetFileName(FilePathAndName) ?? string.Empty;

    public bool HasContent => !string.IsNullOrEmpty(FilePathAndName)
        && !string.IsNullOrEmpty(RepoName)
        && !string.IsNullOrEmpty(OwningAccount);

    public string FilePathAndName { get; set; } = string.Empty;

    public string RepoName { get; set; } = string.Empty;

    public string OwningAccount { get; set; } = string.Empty;

    // Using the resource file for properties like .Text and .Content does not work in this case because
    // the UserControl is inside a DataTemplate and does not have access to the string resource file.
    // any x:Uid used is blank in the view.
    // Set the strings here.
    public string FileFoundMessage { get; } = string.Empty;

    public string FileDescription
    {
        get
        {
            var repoPath = Path.Join(OwningAccount, RepoName);
            return _stringResource.GetLocalized(StringResourceKey.CloneRepoNextStepsDescription, FileName, repoPath);
        }
    }

    public string ViewFileMessage { get; } = string.Empty;

    public string RunFileMessage { get; } = string.Empty;

    public CloneRepoSummaryInformationViewModel(SetupFlowOrchestrator setupFlowOrchestrator, ISetupFlowStringResource stringResource)
    {
        _relatedActivityId = setupFlowOrchestrator.ActivityId;
        _stringResource = stringResource;
        FileFoundMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoNextStepsFileFound);
        ViewFileMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoNextStepsView);
        RunFileMessage = _stringResource.GetLocalized(StringResourceKey.CloneRepoNextStepsRun);
    }

    [RelayCommand]
    public void OpenFileInExplorer()
    {
        TelemetryFactory.Get<ITelemetry>().Log(
            _eventName,
            LogLevel.Critical,
            new CloneRepoNextStepEvent(_viewOperation, RepoName),
            _relatedActivityId);

        if (FilePathAndName is null)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(_viewOperation, new ArgumentNullException(nameof(FilePathAndName)).ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is null when trying to view file.");
            return;
        }

        if (string.IsNullOrEmpty(FilePathAndName))
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(_viewOperation, new ArgumentException($"{nameof(FilePathAndName)} is empty.").ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is empty when trying to view file.");
            return;
        }

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        try
        {
            processStartInfo.FileName = Path.GetDirectoryName(FilePathAndName);
        }
        catch (ArgumentException ex)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
            _eventName,
            LogLevel.Critical,
            new CloneRepoNextStepError(_viewOperation, ex.ToString(), RepoName),
            _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is either empty, contains only white spaces, or contains invalid characters.");
            return;
        }
        catch (PathTooLongException ex)
        {
            TelemetryFactory.Get<ITelemetry>().Log(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(_viewOperation, ex.ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is too long.");
            return;
        }

        StartProcess(processStartInfo, _viewOperation);
    }

    [RelayCommand]
    public void RunInAdminCommandPrompt()
    {
        TelemetryFactory.Get<ITelemetry>().Log(
            _eventName,
            LogLevel.Critical,
            new CloneRepoNextStepEvent(_runOperation, RepoName),
            _relatedActivityId);

        if (FileName is null)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(_runOperation, new ArgumentNullException(nameof(FileName)).ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FileName)} is null when trying to run file.");
            return;
        }

        if (FilePathAndName is null)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(_runOperation, new ArgumentException(nameof(FileName)).ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(FileName)} is null when trying to run file.");
            return;
        }

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;
        processStartInfo.FileName = "winget";
        processStartInfo.ArgumentList.Add("configure");
        processStartInfo.ArgumentList.Add(FilePathAndName);
        processStartInfo.Verb = "RunAs";

        StartProcess(processStartInfo, _runOperation);
    }

    private void StartProcess(ProcessStartInfo processStartInfo, string operation)
    {
        if (processStartInfo is null)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(operation, new ArgumentNullException(nameof(processStartInfo)).ToString(), RepoName),
                _relatedActivityId);

            _log.Warning("CloneRepoSummaryInformationViewModel", $"{nameof(Process)} is null.  Operation {operation}");
            return;
        }

        try
        {
            Process.Start(processStartInfo);
        }
        catch (Win32Exception win32Exception)
        {
            // Usually because the UAC prompt was declined.
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(operation, win32Exception.ToString(), RepoName),
                _relatedActivityId);

            _log.Warning($"An error happened when starting the process.  Operation {operation}", win32Exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(operation, objectDisposedException.ToString(), RepoName),
                _relatedActivityId);

            _log.Warning($"The process object was disposed before it could start.  Operation {operation}", objectDisposedException);
        }
        catch (InvalidOperationException invalidOperationException)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(
                _eventName,
                LogLevel.Critical,
                new CloneRepoNextStepError(operation, invalidOperationException.ToString(), RepoName),
                _relatedActivityId);

            _log.Warning($"An error happened when starting the process.  Operation {operation}", invalidOperationException);
        }
    }
}
