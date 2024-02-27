// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.WinUI;
using DevHome.Common.Environments.Services;
using DevHome.Common.Renderers;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

public class ConfigureTargetTask : ISetupTask, IDisposable
{
    private readonly AutoResetEvent _autoResetEvent = new(false);

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue;

    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly IThemeSelectorService _themeSelectorService;

    public Dictionary<ElementTheme, string> AdaptiveCardHostConfigs { get; set; } = new();

    private readonly Dictionary<ElementTheme, string> _hostConfigFileNames = new()
    {
        { ElementTheme.Dark, "DarkHostConfig.json" },
        { ElementTheme.Light, "LightHostConfig.json" },
    };

    private bool _disposedValue;

    public ActionCenterMessages ActionCenterMessages { get; set; } = new();

    public SDK.IExtensionAdaptiveCardSession2 ExtensionAdaptiveCardSession { get; set; }

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled => false;

    public event ISetupTask.ChangeMessageHandler AddMessage;

    public event ISetupTask.ChangeActionCenterMessageHandler UpdateActionCenterMessage;

    public WinGetConfigFile WingetConfigFileObject { get; set; }

    public string WingetConfigFileString { get; set; }

    public List<ConfigurationUnitResult> ConfigurationResults
    {
        get; private set;
    }

    public uint UserNumberOfAttempts { get; private set; } = 1;

    public uint UserMaxNumberOfAttempts { get; private set; } = 3;

    public SDKApplyConfigurationResult Result { get; private set; }

    public ConfigureTargetTask(
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator,
        IThemeSelectorService themeSelectorService)
    {
        _stringResource = stringResource;
        _computeSystemManager = computeSystemManager;
        _configurationFileBuilder = configurationFileBuilder;
        _themeSelectorService = themeSelectorService;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    }

    public void OnAdaptiveCardSessionChanged(IExtensionAdaptiveCardSession2 cardSession, SDK.ExtensionAdaptiveCardSessionData data)
    {
        if (data == null)
        {
            return;
        }

        if (data.EventKind == SDK.ExtensionAdaptiveCardSessionEventKind.SessionEnded)
        {
            cardSession.SessionStatusChanged -= OnAdaptiveCardSessionChanged;

            ExtensionAdaptiveCardSession = null;
        }
    }

    public void OnApplyConfigurationOperationProgress(object sender, SDK.ConfigurationSetChangeData progressData)
    {
        try
        {
            if (progressData == null)
            {
                Log.Logger?.ReportWarn(Log.Component.ConfigurationTarget, "Unable to get progress of the configuration");
                return;
            }

            if (progressData.CorrectiveActionCardSession != null)
            {
                ExtensionAdaptiveCardSession = progressData.CorrectiveActionCardSession;
                ExtensionAdaptiveCardSession.SessionStatusChanged -= OnAdaptiveCardSessionChanged;

                CreateCorrectiveActionPanel(ExtensionAdaptiveCardSession).GetAwaiter().GetResult();

                AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationActionNeeded, UserNumberOfAttempts++, UserMaxNumberOfAttempts));
                Log.Logger?.ReportWarn(Log.Component.ConfigurationTarget, $"adaptive card receieved from extension");
            }
            else
            {
                var wrapper = new SDKConfigurationSetChangeWrapper(progressData, _stringResource);
                if (wrapper.IsErrorMessagePresent)
                {
                    Log.Logger?.ReportError(Log.Component.ConfigurationTarget, $"Target experienced an error while applying the configuration: {wrapper.GetErrorMessageForLogging()}");
                }

                // AddMessage(wrapper.ToString());
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.ConfigurationTarget, $"Failed to process configuration progress on target machine.", ex);
        }
    }

    public void OnApplyConfigurationOperationCompleted(object sender, SDK.ApplyConfigurationResult applyConfigurationResult)
    {
        // apply configuration set result is used to check if the configuration set was applied successfully, while open configuration
        // set result is used to check if WinGet was able to open the configuration file successfully.
        var applyConfigSetResult = applyConfigurationResult.ApplyConfigurationSetResult;
        var openConfigResult = applyConfigurationResult.OpenConfigurationSetResult;
        var resultCode = applyConfigurationResult.ResultCode;
        var resultInformation = applyConfigurationResult.ResultDescription;

        try
        {
            Result = new SDKApplyConfigurationResult(
                resultCode, resultInformation, new SDKApplyConfigurationSetResult(applyConfigSetResult), new SDKOpenConfigurationSetResult(openConfigResult, _stringResource));

            // Check if there were errors while opening the configuration set.
            if (!Result.OpenConfigSucceeded)
            {
                AddMessage(Result.OpenResult.GetErrorMessage());
                throw new OpenConfigurationSetException(Result.OpenResult.ResultCode, Result.OpenResult.Field, Result.OpenResult.Value);
            }

            if (!Result.ApplyConfigSucceeded)
            {
                throw new SDKApplyConfigurationSetResultException("Unable to get the result of the apply configuration set as it was null.");
            }

            if (Result.ApplyResult.AreConfigUnitsAvailable)
            {
                for (var i = 0; i < Result.ApplyResult.Result.UnitResults.Count; ++i)
                {
                    ConfigurationResults.Add(new ConfigurationUnitResult(Result.ApplyResult.Result.UnitResults[i]));
                }

                Log.Logger?.ReportInfo(Log.Component.ConfigurationTarget, "Successfully applied the configuration");
            }
            else
            {
                throw new SDKApplyConfigurationSetResultException("No configuration units were found. Likely due to an error in the extension.");
            }
        }
        catch (Exception ex)
        {
            Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration on target machine.", ex);
        }

        AddMessage(_stringResource.GetLocalized(StringResourceKey.ConfigureTargetApplyConfigurationStopped, $"{resultInformation}"));
        ContinueExecution();
    }

    private void ContinueExecution()
    {
        // operation is complete, so signal the event.
        _autoResetEvent.Set();
    }

    public IAsyncOperation<TaskFinishedState> Execute()
    {
        return Task.Run(() =>
        {
            try
            {
                UserNumberOfAttempts = 1;
                AddMessage(_stringResource.GetLocalized(StringResourceKey.ApplyingConfigurationMessage));
                WingetConfigFileObject = _configurationFileBuilder.BuildConfigFileObjectFromTaskGroups(_setupFlowOrchestrator.TaskGroups);
                WingetConfigFileString = _configurationFileBuilder.SerializeWingetFileObjectToString(WingetConfigFileObject);
                var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;
                var applyConfigurationOperation = computeSystem.ApplyConfiguration(WingetConfigFileString);

                applyConfigurationOperation.Progress += OnApplyConfigurationOperationProgress;
                applyConfigurationOperation.Completed += OnApplyConfigurationOperationCompleted;

                _autoResetEvent.WaitOne();

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

                if (Result.ResultCode != null)
                {
                    throw Result.ResultCode;
                }

                return TaskFinishedState.Success;
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration on target machine.", e);
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentOperation elevatedComponentOperation) => throw new NotImplementedException();

    TaskMessages ISetupTask.GetLoadingMessages()
    {
        return new()
        {
            Executing = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplying),
            Error = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
            Finished = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccess),
            NeedsReboot = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccessReboot),
        };
    }

    public ActionCenterMessages GetErrorMessages()
    {
        return new()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplyError),
        };
    }

    public ActionCenterMessages GetRebootMessage()
    {
        return new()
        {
            PrimaryMessage = _stringResource.GetLocalized(StringResourceKey.ConfigurationFileApplySuccessReboot),
        };
    }

    public async Task SetupHostConfigFiles()
    {
        try
        {
            foreach (var elementPairing in _hostConfigFileNames)
            {
                var uri = new Uri($"ms-appx:////DevHome.Settings/Assets/{_hostConfigFileNames[elementPairing.Key]}");
                var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                AdaptiveCardHostConfigs.Add(elementPairing.Key, await FileIO.ReadTextAsync(file));
            }

            AdaptiveCardHostConfigs.Add(ElementTheme.Default, AdaptiveCardHostConfigs[ElementTheme.Light]);
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"Failure occurred while retrieving the HostConfig file", ex);
        }
    }

    public async Task CreateCorrectiveActionPanel(IExtensionAdaptiveCardSession2 session)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            await SetupHostConfigFiles();
            var correctiveAction = session;
            var renderer = new AdaptiveCardRenderer();
            var elementTheme = _themeSelectorService.Theme;

            // Add host config for current theme to renderer
            if (AdaptiveCardHostConfigs.TryGetValue(elementTheme, out var hostConfigContents))
            {
                renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"HostConfig file contents are null or empty - HostConfigFileContents: {hostConfigContents}");
            }

            renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

            var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
            extensionAdaptiveCardPanel.Bind(correctiveAction, renderer);
            extensionAdaptiveCardPanel.RequestedTheme = elementTheme;

            if (ActionCenterMessages.ExtensionAdaptiveCardPanel != null)
            {
                ActionCenterMessages.ExtensionAdaptiveCardPanel = null;
                UpdateActionCenterMessage(ActionCenterMessages, ActionMessageRequestKind.Remove);
            }

            ActionCenterMessages.ExtensionAdaptiveCardPanel = extensionAdaptiveCardPanel;
            UpdateActionCenterMessage(ActionCenterMessages, ActionMessageRequestKind.Add);
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _autoResetEvent.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
