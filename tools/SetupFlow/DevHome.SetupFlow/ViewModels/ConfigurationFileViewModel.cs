// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Exceptions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Windows.Storage;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels;

public partial class ConfigurationFileViewModel : SetupPageViewModelBase
{
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

    public ConfigurationFileViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        // Configure navigation bar
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        IsStepPage = false;
    }

    partial void OnReadAndAgreeChanged(bool value)
    {
        Log.Logger?.ReportInfo(Log.Component.Configuration, $"Read and agree changed. Value: {value}");
        CanGoToNextPage = value;
        Orchestrator.NotifyNavigationCanExecuteChanged();
    }

    /// <summary>
    /// Gets the configuration file content
    /// </summary>
    public string Content => Configuration.Content;

    [RelayCommand(CanExecute = nameof(ReadAndAgree))]
    public async Task ConfigureAsAdminAsync()
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
            Log.Logger?.ReportError(Log.Component.Configuration, $"Failed to initialize elevated process.", e);
        }
    }

    [RelayCommand(CanExecute = nameof(ReadAndAgree))]
    public async Task ConfigureAsNonAdminAsync()
    {
        TelemetryFactory.Get<ITelemetry>().Log("ConfigurationButton_Click", LogLevel.Critical, new ConfigureCommandEvent(false), Orchestrator.ActivityId);
        await Orchestrator.GoToNextPage();
    }

    public async Task<bool> LoadFileAsync(StorageFile file)
    {
        return await CheckValidConfigurationFile(file);
    }

    public async Task<bool> CheckValidConfigurationFile(StorageFile file)
    {
        // Check if a file was selected
        if (file == null)
        {
            Log.Logger?.ReportInfo(Log.Component.Configuration, "No configuration file selected");
        }
        else
        {
            try
            {
                Log.Logger?.ReportInfo(Log.Component.Configuration, $"Selected file: {file.Path}");
                Configuration = new (file.Path);
                Orchestrator.FlowTitle = StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, Configuration.Name);
                var task = new ConfigureTask(StringResource, file, Orchestrator.ActivityId);
                await task.OpenConfigurationSetAsync();
                TaskList.Add(task);
                return true;
            }
            catch (OpenConfigurationSetException e)
            {
                // Get the application root window.
                var mainWindow = Application.Current.GetService<WindowEx>();

                Log.Logger?.ReportError(Log.Component.Configuration, $"Opening configuration set failed.", e);
                await mainWindow.ShowErrorMessageDialogAsync(
                    StringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, file.Name),
                    GetErrorMessage(e),
                    StringResource.GetLocalized(StringResourceKey.Close));
            }
            catch (Exception e)
            {
                // Get the application root window.
                var mainWindow = Application.Current.GetService<WindowEx>();

                Log.Logger?.ReportError(Log.Component.Configuration, $"Unknown error while opening configuration set.", e);

                await mainWindow.ShowErrorMessageDialogAsync(
                    file.Name,
                    StringResource.GetLocalized(StringResourceKey.ConfigurationFileOpenUnknownError),
                    StringResource.GetLocalized(StringResourceKey.Close));
            }
        }

        return false;
    }

    /// <summary>
    /// Open file picker to select a YAML configuration file.
    /// </summary>
    /// <returns>True if a YAML configuration file was selected, false otherwise</returns>
    public async Task<bool> PickConfigurationFileAsync()
    {
        // Get the application root window.
        var mainWindow = Application.Current.GetService<WindowEx>();

        // Create and configure file picker
        Log.Logger?.ReportInfo(Log.Component.Configuration, "Launching file picker to select configurationf file");
        var file = await mainWindow.OpenFilePickerAsync(Log.Logger, ("*.yaml;*.yml;*.winget;", StringResource.GetLocalized(StringResourceKey.FilePickerFileTypeOption, "YAML")));

        return await CheckValidConfigurationFile(file);
    }

    private string GetErrorMessage(OpenConfigurationSetException exception)
    {
        return exception.ResultCode?.HResult switch
        {
            OpenConfigurationSetException.WingetConfigErrorInvalidField =>
                StringResource.GetLocalized(StringResourceKey.ConfigurationFieldInvalid, exception.Field),
            OpenConfigurationSetException.WingetConfigErrorUnknownConfigurationFileVersion =>
                StringResource.GetLocalized(StringResourceKey.ConfigurationFileVersionUnknown, exception.Field),
            _ => StringResource.GetLocalized(StringResourceKey.ConfigurationFileInvalid),
        };
    }
}
