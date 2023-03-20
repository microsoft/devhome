// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.ConfigurationFile.Models;
using DevHome.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.ConfigurationFile.ViewModels;

public partial class ConfigurationFileViewModel : SetupPageViewModelBase
{
    private readonly ILogger _logger;
    private readonly IHost _host;
    private readonly SetupFlowOrchestrator _orchestrator;

    public ConfigurationFileViewModel(ILogger logger, SetupFlowStringResource stringResource, IHost host, SetupFlowOrchestrator orchestrator)
        : base(stringResource)
    {
        _logger = logger;
        _host = host;
        _orchestrator = orchestrator;

        // Configure navigation bar
        NextPageButtonText = StringResource.GetLocalized(StringResourceKey.SetUpButton);
        CanGoToNextPage = false;
    }

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

    partial void OnReadAndAgreeChanged(bool value)
    {
        CanGoToNextPage = value;
        _orchestrator.NotifyNavigationCanExecuteChanged();
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

                // TODO Call Configuration COM API once implemented to validate
                // the input file.
                return true;
            }
            catch
            {
                await mainWindow.ShowErrorMessageDialogAsync(
                    StringResource.GetLocalized(StringResourceKey.FileTypeNotSupported),
                    StringResource.GetLocalized(StringResourceKey.ConfigurationFileTypeNotSupported),
                    StringResource.GetLocalized(StringResourceKey.Close));
            }
        }

        return false;
    }
}
