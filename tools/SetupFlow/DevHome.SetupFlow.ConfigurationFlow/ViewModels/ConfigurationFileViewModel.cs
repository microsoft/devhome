// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.ConfigurationFile.Exceptions;
using DevHome.SetupFlow.ConfigurationFile.Models;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.ConfigurationFile.ViewModels;

public partial class ConfigurationFileViewModel : SetupPageViewModelBase
{
    public List<ISetupTask> TaskList { get; } = new List<ISetupTask>();

    /// <summary>
    /// Configuration file
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleText))]
    [NotifyPropertyChangedFor(nameof(Content))]
    private Configuration _configuration;

    /// <summary>
    /// Store the value for whether the agreements are read.
    /// </summary>
    [ObservableProperty]
    private bool _readAndAgree;

    public ConfigurationFileViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator)
        : base(stringResource, orchestrator)
    {
        // Configure navigation bar
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        CanGoToNextPage = false;
        IsStepPage = false;
    }

    partial void OnReadAndAgreeChanged(bool value)
    {
        CanGoToNextPage = value;
        Orchestrator.NotifyNavigationCanExecuteChanged();
    }

    /// <summary>
    /// Gets the title for the configuration page
    /// </summary>
    public string TitleText => StringResource.GetLocalized(StringResourceKey.ViewConfiguration, _configuration.Name);

    /// <summary>
    /// Gets the configuration file content
    /// </summary>
    public string Content => _configuration.Content;

    /// <summary>
    /// Open file picker to select a YAML configuration file.
    /// </summary>
    /// <returns>True if a YAML configuration file was selected, false otherwise</returns>
    public async Task<bool> PickConfigurationFileAsync()
    {
        // Get the application root window.
        var mainWindow = Application.Current.GetService<WindowEx>();

        // Create and configure file picker
        var filePicker = mainWindow.CreateOpenFilePicker();
        filePicker.FileTypeFilter.Add(".yaml");
        filePicker.FileTypeFilter.Add(".yml");
        var file = await filePicker.PickSingleFileAsync();

        // Check if a file was selected
        if (file != null)
        {
            try
            {
                Configuration = new (file.Path);
                var task = new ConfigureTask(StringResource, file);
                await task.OpenConfigurationSetAsync();
                TaskList.Add(task);
                return true;
            }
            catch (OpenConfigurationSetException e)
            {
                await mainWindow.ShowErrorMessageDialogAsync(
                    file.Name,
                    GetErrorMessage(e),
                    StringResource.GetLocalized(StringResourceKey.Close));
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.Configuration, $"Unknown error while opening configuration set: {e.Message}");

                await mainWindow.ShowErrorMessageDialogAsync(
                    file.Name,
                    StringResource.GetLocalized(StringResourceKey.ConfigurationFileOpenUnknownError),
                    StringResource.GetLocalized(StringResourceKey.Close));
            }
        }

        return false;
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
