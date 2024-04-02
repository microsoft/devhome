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
            SendTelemetryAndLogError(_viewOperation, new ArgumentNullException(nameof(FilePathAndName)));
            return;
        }

        if (string.IsNullOrEmpty(FilePathAndName))
        {
            SendTelemetryAndLogError(_viewOperation, new ArgumentNullException(nameof(FilePathAndName)));
            return;
        }

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        try
        {
            // Not catching PathTooLongException.  If the file was in a location that had a too long path,
            // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
            processStartInfo.FileName = Path.GetDirectoryName(FilePathAndName);
        }
        catch (ArgumentException ex)
        {
            SendTelemetryAndLogError(_viewOperation, ex);
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
            SendTelemetryAndLogError(_runOperation, new ArgumentNullException(nameof(FileName)));
            return;
        }

        if (FilePathAndName is null)
        {
            SendTelemetryAndLogError(_runOperation, new ArgumentNullException(nameof(FileName)));
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
        try
        {
            Process.Start(processStartInfo);
        }
        catch (Win32Exception win32Exception)
        {
            // Usually because the UAC prompt was declined.
            SendTelemetryAndLogError(operation, win32Exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            SendTelemetryAndLogError(operation, objectDisposedException);
        }
        catch (InvalidOperationException invalidOperationException)
        {
            SendTelemetryAndLogError(operation, invalidOperationException);
        }
        catch (Exception e)
        {
            SendTelemetryAndLogError(operation, e);
        }
    }

    private void SendTelemetryAndLogError(string operation, Exception e)
    {
        TelemetryFactory.Get<ITelemetry>().LogError(
        _eventName,
        LogLevel.Critical,
        new CloneRepoNextStepError(operation, e.ToString(), RepoName),
        _relatedActivityId);

        _log.Error(e, string.Empty);
    }
}
