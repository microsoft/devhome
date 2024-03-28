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
using DevHome.Common.Views;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.Services;
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

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly AdaptiveCardRenderingService _adaptiveCardRenderingService;

    // Inherited via ISetupTask but unused
    public bool RequiresAdmin => false;

    // Inherited via ISetupTask but unused
    public bool RequiresReboot => false;

    // Inherited via ISetupTask but unused
    public bool DependsOnDevDriveToBeInstalled => false;

    // Inherited via ISetupTask
    public event ISetupTask.ChangeMessageHandler AddMessage;

    // Inherited via ISetupTask
    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;

    public ActionCenterMessages ActionCenterMessages { get; set; } = new() { ExtensionAdaptiveCardPanel = new(), };

    public string ComputeSystemName { get; private set; } = string.Empty;

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

    public ConfigureTargetTask(
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator)
    {
        _stringResource = stringResource;
        _computeSystemManager = computeSystemManager;
        _configurationFileBuilder = configurationFileBuilder;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
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
        _log.Information($"adaptive card receieved from extension");
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

            AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionNeeded, UserNumberOfAttempts++), MessageSeverityKind.Warning);
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
            stringBuilder.AppendLine("---- " + _stringResource.GetLocalized(StringResourceKey.SetupTargetConfigurationProgressUpdate) + " ----");
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
                // We may need to change the formatting of the message in the future.
                var description = BuildConfigurationUnitDescription(wrapper.Unit);
                stringBuilder.AppendLine(GetSpacingForProgressMessage(startingLineNumber++) + description);
                stringBuilder.AppendLine(GetSpacingForProgressMessage(startingLineNumber++) + wrapper.ConfigurationUnitState);
            }
            else
            {
                _log.Information("Extension sent progress but there was no configuration unit data sent.");
            }

            // Example of a message that will be displayed in the UI:
            // ---- Configuration progress recieved! ----
            // There was an issue applying part of the configuration using DSC resource: 'GitClone'.Check the extension's logs
            //      - Assert : GitClone[Clone: wil - C:\Users\Public\Documents\source\repos\wil]
            //            - This part of the configuration is now complete
            AddMessage(stringBuilder.ToString(), severity);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to process configuration progress data on target machine.'{ComputeSystemName}'", ex);
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
                _log.Error($"Extension failed to configure config file with exception. Diagnostic text: {result.DiagnosticText}", result.ExtendedError);
                throw new SDKApplyConfigurationSetResultException(applyConfigurationResult.Result.DiagnosticText);
            }

            // Check if there were errors while opening the configuration set.
            if (!Result.OpenConfigSucceeded)
            {
                AddMessage(Result.OpenResult.GetErrorMessage(), MessageSeverityKind.Error);
                throw new OpenConfigurationSetException(Result.OpenResult.ResultCode, Result.OpenResult.Field, Result.OpenResult.Value);
            }

            // Check if the WinGet apply operation was failed.
            if (!Result.ApplyConfigSucceeded)
            {
                throw new SDKApplyConfigurationSetResultException("Unable to get the result of the apply configuration set as it was null.");
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
                throw new SDKApplyConfigurationSetResultException("No configuration units were found. This is likely due to an error within the extension.");
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to apply configuration on target machine. '{ComputeSystemName}'", ex);
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
                UserNumberOfAttempts = 1;
                var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;
                ComputeSystemName = computeSystem.DisplayName;
                AddMessage(_stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyingConfiguration, ComputeSystemName), MessageSeverityKind.Info);
                WingetConfigFileString = _configurationFileBuilder.BuildConfigFileStringFromTaskGroups(_setupFlowOrchestrator.TaskGroups, ConfigurationFileKind.SetupTarget);
                var applyConfigurationOperation = computeSystem.ApplyConfiguration(WingetConfigFileString);

                applyConfigurationOperation.ConfigurationSetStateChanged += OnApplyConfigurationOperationChanged;
                applyConfigurationOperation.ActionRequired += OnActionRequired;

                // We'll cancell the operation after 10 minutes. This is arbitrary for now and will need to be adjusted in the future.
                // but we'll need to give the user the ability to cancel the operation in the UI as well. This is just a safety net.
                // More work is needed to give the user the ability to cancel the operation as the capability is not currently available.
                // in the UI of Dev Home's Loading page.
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(TimeSpan.FromMinutes(10));

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

                return TaskFinishedState.Success;
            }
            catch (Exception e)
            {
                _log.Error($"Failed to apply configuration on target machine.", e);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation) => throw new NotImplementedException();

    TaskMessages ISetupTask.GetLoadingMessages()
    {
        var localizedTargetName = _stringResource.GetLocalized(StringResourceKey.SetupTargetMachineName);
        var nameToUseInDisplay = string.IsNullOrEmpty(ComputeSystemName) ? localizedTargetName : ComputeSystemName;
        return new()
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyingConfiguration, nameToUseInDisplay),
            Error = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationError, nameToUseInDisplay),
            Finished = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationSuccess, nameToUseInDisplay),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.SetupTargetExtensionApplyConfigurationRebootRequired, nameToUseInDisplay),
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
    /// <param name="session">Adaptive card session sent by the entension when it needs a user to perform an action</param>
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

    private string BuildConfigurationUnitDescription(ConfigurationUnit unit)
    {
        var unitDescription = string.Empty;

        if (unit.Settings?.TryGetValue("description", out var descriptionObj) == true)
        {
            unitDescription = descriptionObj?.ToString() ?? string.Empty;
        }

        if (string.IsNullOrEmpty(unit.Identifier) && string.IsNullOrEmpty(unitDescription))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryMinimal, unit.Intent, unit.Type);
        }

        if (string.IsNullOrEmpty(unit.Identifier))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoId, unit.Intent, unit.Type, unitDescription);
        }

        if (string.IsNullOrEmpty(unitDescription))
        {
            return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryNoDescription, unit.Intent, unit.Type, unit.Identifier);
        }

        return _stringResource.GetLocalized(StringResourceKey.ConfigurationUnitSummaryFull, unit.Intent, unit.Type, unit.Identifier, unitDescription);
    }
}
