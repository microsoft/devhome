// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.SetupFlow.SummaryPage;
using DevHome.Logging;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ViewModels;

public partial class CloneRepoSummaryInformationViewModel : ISummaryInformationViewModel
{
    private const string _eventName = "CloneRepo_NextSteps_Event";

    private const string _runOperation = "run";

    private const string _viewOperation = "view";

    private readonly Guid _relatedActivityId;

    public string FileName
    {
        get
        {
            var fileName = Path.GetFileName(FilePathAndName);

            if (fileName is not null)
            {
                return fileName;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public bool HasContent => !string.IsNullOrEmpty(FilePathAndName) && !string.IsNullOrEmpty(RepoName);

    public string FilePathAndName { get; set; } = string.Empty;

    public string RepoName { get; set; } = string.Empty;

    public CloneRepoSummaryInformationViewModel(IHost host)
    {
        _relatedActivityId = host.GetService<SetupFlowOrchestrator>().ActivityId;
    }

    [RelayCommand]
    public void OpenFileInExplorer()
    {
        TelemetryFactory.Get<ITelemetry>().Log(_eventName, LogLevel.Critical, new CloneRepoNextStepEvent(_viewOperation, RepoName), _relatedActivityId);

        if (FilePathAndName is null)
        {
            TelemetryFactory.Get<ITelemetry>().Log(_eventName, LogLevel.Critical, new CloneRepoNextStepError(_viewOperation, new ArgumentNullException(nameof(FilePathAndName)), RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is null when trying to view file.");
            return;
        }

        if (string.IsNullOrEmpty(FilePathAndName))
        {
            TelemetryFactory.Get<ITelemetry>().Log(_eventName, LogLevel.Critical, new CloneRepoNextStepError(_viewOperation, new ArgumentException($"{nameof(FilePathAndName)} is empty."), RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is empty when trying to view file.");
            return;
        }

        var processStartInfo = new ProcessStartInfo();
        processStartInfo.UseShellExecute = true;

        try
        {
            processStartInfo.FileName = Path.GetDirectoryName(FilePathAndName);
        }
        catch (PathTooLongException ex)
        {
            TelemetryFactory.Get<ITelemetry>().Log(_eventName, LogLevel.Critical, new CloneRepoNextStepError(_viewOperation, ex, RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(FilePathAndName)} is too long.");
            return;
        }

        StartProcess(processStartInfo, _viewOperation);
    }

    [RelayCommand]
    public void RunInAdminCommandPrompt()
    {
        TelemetryFactory.Get<ITelemetry>().Log(_eventName, LogLevel.Critical, new CloneRepoNextStepEvent(_runOperation, RepoName), _relatedActivityId);

        if (FileName is null)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(_runOperation, new ArgumentNullException(nameof(FileName)), RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(FileName)} is null when trying to run file.");
            return;
        }

        if (FilePathAndName is null)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(_runOperation, new ArgumentException(nameof(FileName)), RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(FileName)} is null when trying to run file.");
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
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(operation, new ArgumentNullException(nameof(processStartInfo)), RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportWarn("CloneRepoSummaryInformationViewModel", $"{nameof(Process)} is null.  Operation {operation}");
            return;
        }

        try
        {
            Process.Start(processStartInfo);
        }
        catch (Win32Exception win32Exception)
        {
            // Usually because the UAC prompt was declined.
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(operation, win32Exception, RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportError($"An error happened when starting the process.  Operation {operation}", win32Exception);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(operation, objectDisposedException, RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportError($"THe process object was disposed before it could start.  Operation {operation}", objectDisposedException);
        }
        catch (InvalidOperationException invalidOperationException)
        {
            TelemetryFactory.Get<ITelemetry>().LogError(_eventName, LogLevel.Critical, new CloneRepoNextStepError(operation, invalidOperationException, RepoName), _relatedActivityId);
            GlobalLog.Logger?.ReportError($"An error happened when starting the process.  Operation {operation}", invalidOperationException);
        }
    }
}
