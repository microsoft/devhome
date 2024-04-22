// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Common.Windows.FileDialog;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Serilog;
using Windows.Storage;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class ConfigurationFileViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ConfigurationFileViewModel));
    private readonly IDesiredStateConfiguration _dsc;
    private readonly WindowEx _mainWindow;

    public List<ConfigureTask> TaskList { get; } = new List<ConfigureTask>();

    /// <summary>
    /// Configuration file
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Content))]
    private Configuration _configuration;

    /// <summary>
    /// Store the value for whether the agreements are read.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfigureAsAdminCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfigureAsNonAdminCommand))]
    private bool _readAndAgree;

    [ObservableProperty]
    private IList<DSCConfigurationUnitViewModel> _configurationUnits;

    public ConfigurationFileViewModel(
        ISetupFlowStringResource stringResource,
        IDesiredStateConfiguration dsc,
        WindowEx mainWindow,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        _dsc = dsc;
        _mainWindow = mainWindow;

        // Configure navigation bar
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        IsStepPage = false;
    }

    partial void OnReadAndAgreeChanged(bool value)
    {
        _log.Information($"Read and agree changed. Value: {value}");
        CanGoToNextPage = value;
        Orchestrator.NotifyNavigationCanExecuteChanged();
    }

    /// <summary>
    /// Gets the configuration file content
    /// </summary>
    public string Content => Configuration.Content;

    [RelayCommand(CanExecute = nameof(ReadAndAgree))]
    private async Task ConfigureAsAdminAsync()
    {
        foreach (var task in TaskList)
        {
            task.RequiresAdmin = true;
        }

        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationButton_Click", LogLevel.Critical, new ConfigureCommandEvent(true), Orchestrator.ActivityId);
        try
        {
            await Orchestrator.InitializeElevatedServerAsync();
            await Orchestrator.GoToNextPage();
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to initialize elevated process.");
        }
    }

    [RelayCommand(CanExecute = nameof(ReadAndAgree))]
    private async Task ConfigureAsNonAdminAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationButton_Click", LogLevel.Critical, new ConfigureCommandEvent(false), Orchestrator.ActivityId);
        await Orchestrator.GoToNextPage();
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        try
        {
            if (Configuration != null && ConfigurationUnits == null)
            {
                var configUnits = await _dsc.GetConfigurationUnitDetailsAsync(Configuration, Orchestrator.ActivityId);
                ConfigurationUnits = configUnits.Select(u => new DSCConfigurationUnitViewModel(u)).ToList();
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to get configuration unit details.");
        }
    }

    /// <summary>
    /// Open file picker to select a YAML configuration file.
    /// </summary>
    /// <returns>True if a YAML configuration file was selected, false otherwise</returns>
    public async Task<bool> PickConfigurationFileAsync()
    {
        try
        {
            // Create and configure file picker
            _log.Information("Launching file picker to select configuration file");
            using var fileDialog = new WindowOpenFileDialog();
            fileDialog.AddFileType(StringResource.GetLocalized(StringResourceKey.FilePickerFileTypeOption, "YAML"), ".yaml", ".yml", ".winget");
            var file = await fileDialog.ShowAsync(_mainWindow);
            return await LoadConfigurationFileInternalAsync(file);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to open file picker.");
            return false;
        }
    }

    /// <summary>
    /// Load a configuration file if feature is enabled
    /// </summary>
    /// <param name="file">The configuration file to load</param>
    /// <returns>True if the configuration file was loaded, false otherwise</returns>
    public async Task<bool> LoadFileAsync(StorageFile file)
    {
        _log.Information("Loading a configuration file");
        if (!await _dsc.IsUnstubbedAsync())
        {
            await _mainWindow.ShowErrorMessageDialogAsync(
                StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, file.Name),
                StringResource.GetLocalized(StringResourceKey.ConfigurationActivationFailedDisabled),
                StringResource.GetLocalized(StringResourceKey.Close));
            return false;
        }

        return await LoadConfigurationFileInternalAsync(file);
    }

    /// <summary>
    /// Core logic to load a configuration file
    /// </summary>
    /// <param name="file">The configuration file to load</param>
    /// <returns>True if the configuration file was loaded, false otherwise</returns>
    private async Task<bool> LoadConfigurationFileInternalAsync(StorageFile file)
    {
        // Check if a file was selected
        if (file == null)
        {
            _log.Information("No configuration file selected");
            return false;
        }

        try
        {
            _log.Information($"Selected file: {file.Path}");
            Configuration = new(file.Path);
            Orchestrator.FlowTitle = StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, Configuration.Name);
            await _dsc.ValidateConfigurationAsync(file.Path, Orchestrator.ActivityId);
            TaskList.Add(new(StringResource, _dsc, file, Orchestrator.ActivityId));
            return true;
        }
        catch (OpenConfigurationSetException e)
        {
            _log.Error(e, $"Opening configuration set failed.");
            await _mainWindow.ShowErrorMessageDialogAsync(
                StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, file.Name),
                GetErrorMessage(e),
                StringResource.GetLocalized(StringResourceKey.Close));
        }
        catch (Exception e)
        {
            _log.Error(e, $"Unknown error while opening configuration set.");

            await _mainWindow.ShowErrorMessageDialogAsync(
                file.Name,
                StringResource.GetLocalized(StringResourceKey.ConfigurationFileOpenUnknownError),
                StringResource.GetLocalized(StringResourceKey.Close));
        }

        return false;
    }

    private string GetErrorMessage(OpenConfigurationSetException exception)
    {
        switch (exception.ResultCode.HResult)
        {
            case WinGetConfigurationException.WingetConfigErrorInvalidFieldType:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldInvalidType, exception.Field);
            case WinGetConfigurationException.WingetConfigErrorInvalidFieldValue:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldInvalidValue, exception.Field, exception.Value);
            case WinGetConfigurationException.WingetConfigErrorMissingField:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldMissing, exception.Field);
            case WinGetConfigurationException.WingetConfigErrorUnknownConfigurationFileVersion:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFileVersionUnknown, exception.Value);
            case WinGetConfigurationException.WingetConfigErrorInvalidConfigurationFile:
            case WinGetConfigurationException.WingetConfigErrorInvalidYaml:
            default:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFileInvalid);
        }
    }
}
