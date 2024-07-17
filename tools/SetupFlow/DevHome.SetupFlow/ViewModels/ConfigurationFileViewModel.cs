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
using DevHome.Services.DesiredStateConfiguration.Contracts;
using DevHome.Services.DesiredStateConfiguration.Exceptions;
using DevHome.Services.DesiredStateConfiguration.Models;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace DevHome.SetupFlow.ViewModels;

public partial class ConfigurationFileViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly IDSC _dsc;
    private readonly Window _mainWindow;

    public List<ConfigureTask> TaskList { get; } = new List<ConfigureTask>();

    /// <summary>
    /// Configuration file
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Content))]
    private IDSCFile _configuration;

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
        ILogger<ConfigurationFileViewModel> logger,
        ISetupFlowStringResource stringResource,
        IDSC dsc,
        Window mainWindow,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        _logger = logger;
        _dsc = dsc;
        _mainWindow = mainWindow;

        // Configure navigation bar
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        IsStepPage = false;
    }

    partial void OnReadAndAgreeChanged(bool value)
    {
        _logger.LogInformation($"Read and agree changed. Value: {value}");
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

        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationButton_Click", Telemetry.LogLevel.Critical, new ConfigureCommandEvent(true), Orchestrator.ActivityId);
        try
        {
            await Orchestrator.InitializeElevatedServerAsync();
            await Orchestrator.GoToNextPage();
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to initialize elevated process.");
        }
    }

    [RelayCommand(CanExecute = nameof(ReadAndAgree))]
    private async Task ConfigureAsNonAdminAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationButton_Click", Telemetry.LogLevel.Critical, new ConfigureCommandEvent(false), Orchestrator.ActivityId);
        await Orchestrator.GoToNextPage();
    }

    [RelayCommand]
    private async Task OnLoadedAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_Loaded", Telemetry.LogLevel.Critical, new EmptyEvent(PartA_PrivTags.ProductAndServicePerformance), Orchestrator.ActivityId);
        try
        {
            if (Configuration != null && ConfigurationUnits == null)
            {
                var configSet = await _dsc.GetConfigurationUnitDetailsAsync(Configuration);
                ConfigurationUnits = configSet.Units.Select(u => new DSCConfigurationUnitViewModel(u)).ToList();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to get configuration unit details.");
        }
    }

    [RelayCommand]
    private void OnViewSelectionChanged(string newViewMode)
    {
        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationFile_ViewSelectionChanged", Telemetry.LogLevel.Critical, new ConfigureModeCommandEvent(newViewMode), Orchestrator.ActivityId);
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
            _logger.LogInformation("Launching file picker to select configuration file");
            using var fileDialog = new WindowOpenFileDialog();
            fileDialog.AddFileType(StringResource.GetLocalized(StringResourceKey.FilePickerFileTypeOption, "YAML"), ".yaml", ".yml", ".winget");
            var file = await fileDialog.ShowAsync(_mainWindow);
            return await LoadConfigurationFileInternalAsync(file);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Failed to open file picker.");
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
        _logger.LogInformation("Loading a configuration file");
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
            _logger.LogInformation("No configuration file selected");
            return false;
        }

        try
        {
            _logger.LogInformation($"Selected file: {file.Path}");
            Configuration = await DSCFile.LoadAsync(file.Path);
            Orchestrator.FlowTitle = StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, Configuration.Name);
            await _dsc.ValidateConfigurationAsync(Configuration);
            TaskList.Add(new(StringResource, _dsc, Configuration, Orchestrator.ActivityId));
            return true;
        }
        catch (OpenConfigurationSetException e)
        {
            _logger.LogError(e, $"Opening configuration set failed.");
            await _mainWindow.ShowErrorMessageDialogAsync(
                StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, file.Name),
                GetErrorMessage(e),
                StringResource.GetLocalized(StringResourceKey.Close));
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unknown error while opening configuration set.");

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
            case ConfigurationException.WingetConfigErrorInvalidFieldType:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldInvalidType, exception.Field);
            case ConfigurationException.WingetConfigErrorInvalidFieldValue:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldInvalidValue, exception.Field, exception.Value);
            case ConfigurationException.WingetConfigErrorMissingField:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFieldMissing, exception.Field);
            case ConfigurationException.WingetConfigErrorUnknownConfigurationFileVersion:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFileVersionUnknown, exception.Value);
            case ConfigurationException.WingetConfigErrorInvalidConfigurationFile:
            case ConfigurationException.WingetConfigErrorInvalidYaml:
            default:
                return StringResource.GetLocalized(StringResourceKey.ConfigurationFileInvalid);
        }
    }
}
