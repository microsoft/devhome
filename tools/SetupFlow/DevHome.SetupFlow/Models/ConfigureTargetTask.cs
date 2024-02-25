// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

extern alias Projection;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Environments.Services;
using DevHome.Common.Renderers;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Projection::DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;
using Windows.Storage;

using SDK = Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models;

public class ConfigureTargetTask : ISetupTask, IDisposable
{
    private readonly AutoResetEvent _autoResetEvent = new(false);

    private readonly ISetupFlowStringResource _stringResource;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly IThemeSelectorService _themeSelectorService;

    private bool _disposedValue;

    public ExtensionAdaptiveCardPanel CorrectiveExtensionCardPanel { get; set; }

    public SDK.IExtensionAdaptiveCardSession2 ExtensionAdaptiveCardSession { get; set; }

    public event EventHandler<ExtensionAdaptiveCardPanel> CorrectiveExtensionAdaptiveCardPanelRemoved;

    public event EventHandler<ExtensionAdaptiveCardPanel> CorrectiveExtensionAdaptiveCardPanelAdded;

    public bool RequiresAdmin => false;

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled => false;

    public event ISetupTask.ChangeMessageHandler AddMessage;

    public bool IsExecuting { get; private set; }

    public WinGetConfigFile WingetConfigFileObject { get; set; }

    public string WingetConfigFileString { get; set; }

    public List<ConfigurationUnitResult> ConfigurationResults
    {
        get; private set;
    }

    public SDK.ApplyConfigurationResult Result { get; private set; }

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
            CorrectiveExtensionAdaptiveCardPanelRemoved?.Invoke(this, CorrectiveExtensionCardPanel);
            ExtensionAdaptiveCardSession = null;
        }
    }

    public void OnApplyConfigurationOperationProgress(object sender, SDK.ConfigurationSetChangeData progressData)
    {
        if (progressData == null)
        {
            return;
        }

        if (progressData.CorrectiveActionCardSession != null)
        {
            ExtensionAdaptiveCardSession.SessionStatusChanged -= OnAdaptiveCardSessionChanged;
            CorrectiveExtensionAdaptiveCardPanelRemoved?.Invoke(this, CorrectiveExtensionCardPanel);
            CorrectiveExtensionCardPanel = CreateCorrectiveActionPanel(progressData.CorrectiveActionCardSession).GetAwaiter().GetResult();
            CorrectiveExtensionAdaptiveCardPanelAdded?.Invoke(this, CorrectiveExtensionCardPanel);
        }
        else
        {
            var wrapper = new SDKConfigurationSetChangeWrapper(progressData, _stringResource);
            if (wrapper.IsErrorMessagePresent)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Target experienced an error while applying the configuration: {wrapper.GetErrorMessageForLogging()}");
            }

            AddMessage(wrapper.ToString());
        }
    }

    public void OnApplyConfigurationOperationCompleted(object sender, SDK.ApplyConfigurationResult applyConfigurationResult)
    {
        if (applyConfigurationResult.OpenConfigurationSetResult != null && applyConfigurationResult.OpenConfigurationSetResult.ResultCode.HResult != 0)
        {
            var openConfigResult = applyConfigurationResult.OpenConfigurationSetResult;
            var openConfigurationException = new OpenConfigurationSetException(openConfigResult.ResultCode, openConfigResult.Field, openConfigResult.Value);
            Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to open configuration set on target.", openConfigurationException);

            AddMessage(new SDKOpenConfigurationSetResult(openConfigResult, _stringResource).ToString());
        }

        var unitResults = applyConfigurationResult.ApplyConfigurationSetResult.UnitResults;
        for (var i = 0; i < unitResults.Count; ++i)
        {
            ConfigurationResults.Add(new ConfigurationUnitResult(unitResults[i]));
        }

        Result = applyConfigurationResult;
        Console.WriteLine("ApplyConfigurationOperationCompleted");

        // operation is complete, so signal the event.
        IsExecuting = false;
        _autoResetEvent.Set();
    }

    public IAsyncOperation<TaskFinishedState> Execute()
    {
        return Task.Run(() =>
        {
            try
            {
                AddMessage(_stringResource.GetLocalized(StringResourceKey.ApplyingConfigurationMessage));
                WingetConfigFileObject = _configurationFileBuilder.BuildConfigFileObjectFromTaskGroups(_setupFlowOrchestrator.TaskGroups);
                WingetConfigFileString = _configurationFileBuilder.SerializeWingetFileObjectToString(WingetConfigFileObject);
                var computeSystem = _computeSystemManager.ComputeSystemSetupItem.ComputeSystemToSetup;
                IsExecuting = true;
                var applyConfigurationOperation = computeSystem.ApplyConfiguration(WingetConfigFileString);

                applyConfigurationOperation.Progress += OnApplyConfigurationOperationProgress;
                applyConfigurationOperation.Completed += OnApplyConfigurationOperationCompleted;

                while (IsExecuting)
                {
                    _autoResetEvent.WaitOne();
                }

                if (Result.ResultCode == null)
                {
                    return TaskFinishedState.Success;
                }
                else
                {
                    throw Result.ResultCode;
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to apply configuration on target machine.", e);
                _autoResetEvent.Set();
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

    public async Task<ExtensionAdaptiveCardPanel> CreateCorrectiveActionPanel(IExtensionAdaptiveCardSession2 session)
    {
        var correctiveAction = session;
        var renderer = new AdaptiveCardRenderer();

        // Add custom Adaptive Card renderer for LoginUI as done for Widgets.
        renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());

        var hostConfigContents = string.Empty;
        var elementTheme = _themeSelectorService.Theme;
        var hostConfigFileName = (elementTheme == ElementTheme.Light) ? "LightHostConfig.json" : "DarkHostConfig.json";
        try
        {
            var uri = new Uri($"ms-appx:////DevHome.Settings/Assets/{hostConfigFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            hostConfigContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"Failure occurred while retrieving the HostConfig file - HostConfigFileName: {hostConfigFileName}.", ex);
        }

        // Add host config for current theme to renderer
        if (!string.IsNullOrEmpty(hostConfigContents))
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

        return extensionAdaptiveCardPanel;
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
