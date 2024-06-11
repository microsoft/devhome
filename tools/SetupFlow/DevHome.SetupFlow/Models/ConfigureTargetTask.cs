// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.Environments;
using DevHome.Common.TelemetryEvents.SetupFlow.Environments;
using DevHome.Common.Views;
using DevHome.Services.DesiredStateConfiguration.Exceptions;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Serilog;
using Windows.Foundation;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

public class ConfigureTargetTask : ISetupTask
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ConfigureTargetTask));

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly AdaptiveCardRenderingService _adaptiveCardRenderingService;

    // Inherited via ISetupTask but unused
    public bool RequiresAdmin => false;

    // Inherited via ISetupTask but unused
    public bool RequiresReboot => false;

    // Inherited via ISetupTask
    public string TargetName => string.IsNullOrEmpty(ComputeSystemName) ?
            _stringResource.GetLocalized(StringResourceKey.SetupTargetMachineName) :
            ComputeSystemName;

    // Inherited via ISetupTask but unused
    public bool DependsOnDevDriveToBeInstalled => false;

    // Inherited via ISetupTask
    public event ISetupTask.ChangeMessageHandler AddMessage;

    // Inherited via ISetupTask
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;

    public ActionCenterMessages ActionCenterMessages { get; set; } = new() { ExtensionAdaptiveCardPanel = new(), };

    public string ComputeSystemName => _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup.DisplayName.Value ?? string.Empty;

    public SDK.IExtensionAdaptiveCardSession2 ExtensionAdaptiveCardSession { get; private set; }

    public string WingetConfigFileString { get; set; }

    /// <summary>
    /// Gets the results of the configuration units that were applied to the target machine. These results are
    /// what we will display to the user in the summary page, assuming the extension was able to start Winget
    /// configure and send back the results to us.
    /// </summary>
    public List<ConfigurationUnitResult> ConfigurationResults { get; private set; } = new();

    public uint UserNumberOfAttempts { get; private set; } = 1;

    public uint UserMaxNumberOfAttempts { get; private set; } = 3;

    /// <summary>
    /// Gets the result of the apply configuration operation.
    /// </summary>
    public SDKApplyConfigurationResult Result { get; private set; }

    public IAsyncOperation<ApplyConfigurationResult> ApplyConfigurationAsyncOperation { get; private set; }

    public ISummaryInformationViewModel SummaryScreenInformation { get; }

    public ConfigureTargetTask(
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator,
        DispatcherQueue dispatcherQueue)
    {
        _stringResource = stringResource;
        _computeSystemManager = computeSystemManager;
        _configurationFileBuilder = configurationFileBuilder;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        _dispatcherQueue = dispatcherQueue;
        _adaptiveCardRenderingService = Application.Current.GetService<AdaptiveCardRenderingService>();
    }

    public void OnAdaptiveCardSessionStopped(IExtensionAdaptiveCardSession2 cardSession, SDK.ExtensionAdaptiveCardSessionStoppedEventArgs data)
    {
        _log.Information("Extension ending adaptive card session");

        // Now that the session has ended, we can remove the adaptive card panel from the UI.
        cardSession.Stopped -= OnAdaptiveCardSessionStopped;
        RemoveAdaptiveCardPanelFromLoadingUI();
        ExtensionAdaptiveCardSession = null;

        // if the session ended successfully we should relay this to the user.
        if (data.Result.Status == SDK.ProviderOperationStatus.Success)
        {
            AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionSuccess), MessageSeverityKind.Success);
        }
        else
        {
            if (UserNumberOfAttempts <= UserMaxNumberOfAttempts)
            {
                AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionFailureRetry), MessageSeverityKind.Warning);
                return;
            }

            AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionFailureEnd), MessageSeverityKind.Error);
            _log.Error("Error no more attempts to correct action");
        }
    }

    public void OnActionRequired(IApplyConfigurationOperation operation, SDK.ApplyConfigurationActionRequiredEventArgs actionRequiredEventArgs)
    {
        _log.Information($"adaptive card received from extension");
        var correctiveCard = actionRequiredEventArgs?.CorrectiveActionCardSession;

        if (correctiveCard != null)
        {
            // If the extension sends a new adaptive card session, we need to update the session and the UI.
            if (ExtensionAdaptiveCardSession != null)
            {
                RemoveAdaptiveCardPanelFromLoadingUI();
                ExtensionAdaptiveCardSession.Stopped -= OnAdaptiveCardSessionStopped;
            }

            ExtensionAdaptiveCardSession = correctiveCard;
            ExtensionAdaptiveCardSession.Stopped += OnAdaptiveCardSessionStopped;

            CreateCorrectiveActionPanel(ExtensionAdaptiveCardSession).GetAwaiter().GetResult();

            AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionNeeded, UserNumberOfAttempts++, UserMaxNumberOfAttempts), MessageSeverityKind.Warning);
        }
        else
        {
            _log.Information("A corrective action was sent from the extension but the adaptive card session was null.");
        }
    }

    public void OnApplyConfigurationOperationChanged(object sender, SDK.ConfigurationSetStateChangedEventArgs changeEventArgs)
    {
        try
        {
            var progressData = changeEventArgs.ConfigurationSetChangeData;

            if (progressData == null)
            {
                _log.Error("Unable to get progress of the configuration as the progress data was null");
                return;
            }

            var severity = MessageSeverityKind.Info;

            // Adaptive card session was not sent, so we check if there are any errors or due to applying a configuration unit/set.
            var wrapper = new SDKConfigurationSetChangeWrapper(progressData, _stringResource);
            var potentialErrorMsg = wrapper.GetErrorMessagesForDisplay();
            var stringBuilder = new StringBuilder();
            var startingLineNumber = 0u;

            if (wrapper.Change == SDK.ConfigurationSetChangeEventType.SetStateChanged)
            {
                // Configuration set changed
                stringBuilder.AppendLine(GetSpacingForProgressMessage(startingLineNumber++) + wrapper.ConfigurationSetState);
            }

            if (wrapper.IsErrorMessagePresent)
            {
                _log.Error($"Target experienced an error while applying the configuration: {wrapper.GetErrorMessageForLogging()}");
                severity = MessageSeverityKind.Error;
                stringBuilder.AppendLine(GetSpacingForProgressMessage(startingLineNumber++) + wrapper.GetErrorMessagesForDisplay());
            }

            // In the future we need to add more messaging to the UI for the user to understand what is happening. It is on the extension to provide
            // us with this messaging. Right now we only get error information and information about which configuration units are/were applied. However
            // there is no way for us to know what the extension is doing, it may not have started configuration yet but may simply be installing prerequisites.
            if (wrapper.Unit != null)
            {
                // Showing "pending" unit states is not useful to the user, so we'll ignore them.
                if (wrapper.UnitState != ConfigurationUnitState.Pending)
                {
                    var description = BuildConfigurationUnitDescription(wrapper.Unit);
                    stringBuilder.AppendLine(description.packageIdDescription);
                    if (!string.IsNullOrEmpty(description.packageNameDescription))
                    {
                        stringBuilder.AppendLine(description.packageNameDescription);
                    }

                    stringBuilder.AppendLine(wrapper.ConfigurationUnitState);
                    if ((wrapper.UnitState == ConfigurationUnitState.Completed) && !wrapper.IsErrorMessagePresent)
                    {
                        severity = MessageSeverityKind.Success;
                    }
                }
                else
                {
                    _log.Information("Ignoring configuration unit pending state.");
                }
            }
            else
            {
                _log.Information("Extension sent progress but there was no configuration unit data sent.");
            }

            // Examples of a message that will be displayed in the UI:
            // Apply: WinGetPackage [Microsoft.VisualStudioCode]
            // Install: Microsoft Visual Studio Code
            // Configuration applied
            //
            // There was an issue applying part of the configuration using DSC resource: 'WinGetPackage'.Error: WinGetPackage Failed installing Notepad++.Notepad++.
            // InstallStatus 'InstallError' InstallerErrorCode '0' ExtendedError '-2147023673'
            // Apply: WinGetPackage[Notepad++.Notepad++]
            // Install: Notepad++
            // Configuration applied
            if (stringBuilder.Length > 0)
            {
                AddMessage(stringBuilder.ToString(), severity);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to process configuration progress data on target machine.'{ComputeSystemName}'");
        }
    }

    private string GetSpacingForProgressMessage(uint lineNumber)
    {
        if (lineNumber == 0)
        {
            return string.Empty;
        }

        var spacing = string.Empty;

        // Add 6 spaces for each line number.
        for (var i = 0; i < lineNumber; ++i)
        {
            spacing += "      ";
        }

        // now add a dash to the end of the spacing to make it look like a bullet point.
        spacing += "- ";
        return spacing;
    }

    public void HandleCompletedOperation(SDK.ApplyConfigurationResult applyConfigurationResult)
    {
        // apply configuration set result is used to check if the configuration set was applied successfully, while open configuration
        // set result is used to check if WinGet was able to open the configuration file successfully.
        var applyConfigSetResult = applyConfigurationResult.ApplyConfigurationSetResult;
        var openConfigResult = applyConfigurationResult.OpenConfigurationSetResult;
        var resultStatus = applyConfigurationResult.Result.Status;
        var result = applyConfigurationResult.Result;
        var resultInformation = new string(result.DisplayMessage);

        try
        {
            Result = new SDKApplyConfigurationResult(result, new SDKApplyConfigurationSetResult(applyConfigSetResult), new SDKOpenConfigurationSetResult(openConfigResult, _stringResource));

            if (resultStatus == ProviderOperationStatus.Failure)
            {
                _log.Error(result.ExtendedError, $"Extension failed to configure config file with exception. Diagnostic text: {result.DiagnosticText}");
                throw new SDKApplyConfigurationSetResultException(applyConfigurationResult.Result.DiagnosticText);
            }

            // Check if there were errors while opening the configuration set.
            if (!Result.OpenConfigSucceeded)
            {
                AddMessage(Result.OpenResult.GetErrorMessage(), MessageSeverityKind.Error);
                throw new OpenConfigurationSetException(Result.OpenResult.ResultCode, Result.OpenResult.Field, Result.OpenResult.Value);
            }

            // Gather the configuration results. We'll display these to the user in the summary page if they are available.
            if (Result.ApplyResult.AreConfigUnitsAvailable)
            {
                for (var i = 0; i < Result.ApplyResult.Result.UnitResults.Count; ++i)
                {
                    ConfigurationResults.Add(new ConfigurationUnitResult(Result.ApplyResult.Result.UnitResults[i]));
                }

                _log.Information("Configuration stopped");
            }
            else
            {
                // Check if the WinGet apply operation failed.
                if (Result.ApplyResult.ResultException != null)
                {
                    // TODO: We should propagate this error to Summery page.
                    throw Result.ApplyResult.ResultException;
                }
                else if (!Result.ApplyConfigSucceeded)
                {
                    // Failed, but no configuration units and no result exception. Something is wrong with result reporting.
                    throw new SDKApplyConfigurationSetResultException("Unable to get the result of the apply configuration set as it was null.");
                }

                // Succeeded, but no configuration units. Something is wrong with result reporting.
                throw new SDKApplyConfigurationSetResultException("No configuration units were found. This is likely due to an error within the extension.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to apply configuration on target machine. '{ComputeSystemName}'");
        }

        var tempResultInfo = !string.IsNullOrEmpty(resultInformation) ? resultInformation : string.Empty;
        var severity = Result.ApplyConfigSucceeded ? MessageSeverityKind.Info : MessageSeverityKind.Error;

        if (string.IsNullOrEmpty(tempResultInfo))
        {
            AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationStoppedWithNoEndingMessage), severity);
            return;
        }

        AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationStopped, $"{tempResultInfo}"), severity);
    }

    /// <summary>
    /// Signals to the loading page view model that the adaptive card panel should be removed from the UI.
    /// </summary>
    public void RemoveAdaptiveCardPanelFromLoadingUI()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (ActionCenterMessages.ExtensionAdaptiveCardPanel != null)
            {
                ActionCenterMessages.ExtensionAdaptiveCardPanel = null;
                UpdateActionCenterMessage(ActionCenterMessages, ActionMessageRequestKind.Remove);
            }
        });
    }

    public IAsyncOperation<TaskFinishedState> Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                _log.Information($"Starting configuration on {ComputeSystemName}");
                UserNumberOfAttempts = 1;
                AddMessage(_stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyingConfiguration, ComputeSystemName), MessageSeverityKind.Info);
                WingetConfigFileString = _configurationFileBuilder.BuildConfigFileStringFromTaskGroups(_setupFlowOrchestrator.TaskGroups, ConfigurationFileKind.SetupTarget);
                var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;
                var applyConfigurationOperation = computeSystem.CreateApplyConfigurationOperation(WingetConfigFileString);

                applyConfigurationOperation.ConfigurationSetStateChanged += OnApplyConfigurationOperationChanged;
                applyConfigurationOperation.ActionRequired += OnActionRequired;

                // We'll cancel the operation after 10 minutes. This is arbitrary for now and will need to be adjusted in the future.
                // but we'll need to give the user the ability to cancel the operation in the UI as well. This is just a safety net.
                // More work is needed to give the user the ability to cancel the operation as the capability is not currently available.
                // in the UI of Dev Home's Loading page.
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(TimeSpan.FromMinutes(10));

                TelemetryFactory.Get<ITelemetry>().Log(
                    "Environment_OperationInvoked_Event",
                    LogLevel.Measure,
                    new EnvironmentOperationUserEvent(EnvironmentsTelemetryStatus.Started, ComputeSystemOperations.ApplyConfiguration, computeSystem.AssociatedProviderId.Value, string.Empty, _setupFlowOrchestrator.ActivityId));

                ApplyConfigurationAsyncOperation = applyConfigurationOperation.StartAsync();
                var result = await ApplyConfigurationAsyncOperation.AsTask().WaitAsync(tokenSource.Token);

                applyConfigurationOperation.ConfigurationSetStateChanged -= OnApplyConfigurationOperationChanged;
                applyConfigurationOperation.ActionRequired -= OnActionRequired;

                HandleCompletedOperation(result);

                var openConFigException = Result.OpenResult.ResultCode;
                var applyConfigException = Result.ApplyResult.ResultException;

                if (openConFigException != null)
                {
                    throw openConFigException;
                }

                if (applyConfigException != null)
                {
                    throw applyConfigException;
                }

                if (Result.ProviderResult.Status != ProviderOperationStatus.Success)
                {
                    throw Result.ProviderResult.ExtendedError ?? throw new SDKApplyConfigurationSetResultException("Applying the configuration failed but we weren't able to check the ProviderOperation results extended error.");
                }

                LogCompletionTelemetry(TaskFinishedState.Success);
                return TaskFinishedState.Success;
            }
            catch (Exception e)
            {
                _log.Error(e, $"Failed to apply configuration on target machine.");
                LogCompletionTelemetry(TaskFinishedState.Failure);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation) => throw new NotImplementedException();

    TaskMessages ISetupTask.GetLoadingMessages()
    {
        return new()
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyingConfiguration, TargetName),
            Error = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationError, TargetName),
            Finished = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationSuccess, TargetName),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationRebootRequired, TargetName),
        };
    }

    public ActionCenterMessages GetErrorMessages()
    {
        return new()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationError, ComputeSystemName),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationRebootRequired, ComputeSystemName),
        };
    }

    /// <summary>
    /// Creates the adaptive card that will appear in the action center of the loading page.
    /// The theming for the adaptive card isn't dynamic but in the future we can make it so.
    /// </summary>
    /// <param name="session">Adaptive card session sent by the extension when it needs a user to perform an action</param>
    public async Task CreateCorrectiveActionPanel(IExtensionAdaptiveCardSession2 session)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var renderer = await _adaptiveCardRenderingService.GetRendererAsync();

            var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
            extensionAdaptiveCardPanel.Bind(session, renderer);

            if (ActionCenterMessages.ExtensionAdaptiveCardPanel != null)
            {
                ActionCenterMessages.ExtensionAdaptiveCardPanel = null;
                UpdateActionCenterMessage(ActionCenterMessages, ActionMessageRequestKind.Remove);
            }

            ActionCenterMessages.ExtensionAdaptiveCardPanel = extensionAdaptiveCardPanel;
            UpdateActionCenterMessage(ActionCenterMessages, ActionMessageRequestKind.Add);
        });
    }

    private (string packageIdDescription, string packageNameDescription) BuildConfigurationUnitDescription(ConfigurationUnit unit)
    {
        var unitDescription = string.Empty;

        if (unit.Settings?.TryGetValue("description", out var descriptionObj) == true)
        {
            unitDescription = descriptionObj?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(unit.Identifier) && string.IsNullOrEmpty(unitDescription))
        {
            return (_stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryMinimal, unit.Intent, unit.Type), string.Empty);
        }

        if (string.IsNullOrEmpty(unit.Identifier))
        {
            return (_stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoId, unit.Intent, unit.Type, unitDescription), string.Empty);
        }

        var descriptionParts = unit.Identifier.Split(ConfigurationFileBuilder.PackageNameSeparator);
        var packageId = descriptionParts[0];
        var packageName = string.Empty;
        if (descriptionParts.Length > 1)
        {
            packageName = $"Install: {descriptionParts[1]}";
        }

        if (string.IsNullOrEmpty(unitDescription))
        {
            return (_stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoDescription, unit.Intent, unit.Type, packageId), packageName);
        }

        return (_stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryFull, unit.Intent, unit.Type, packageId, unitDescription), packageName);
    }

    private void LogCompletionTelemetry(TaskFinishedState taskFinishedState)
    {
        var status = taskFinishedState == TaskFinishedState.Success ? EnvironmentsTelemetryStatus.Succeeded : EnvironmentsTelemetryStatus.Failed;
        var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;

        TelemetryFactory.Get<ITelemetry>().Log(
            "Environment_OperationInvoked_Event",
            LogLevel.Measure,
            new EnvironmentOperationUserEvent(status, ComputeSystemOperations.ApplyConfiguration, computeSystem.AssociatedProviderId.Value, string.Empty, _setupFlowOrchestrator.ActivityId));
    }
}
