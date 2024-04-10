// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace DevHome.Services;

/// <summary>
/// Class that handles the activation of the application when a DSC file (*.winget) is opened
/// </summary>
public class DSCFileActivationHandler : ActivationHandler<FileActivatedEventArgs>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DSCFileActivationHandler));
    private const string WinGetFileExtension = ".winget";
    private readonly ISetupFlowStringResource _setupFlowStringResource;
    private readonly INavigationService _navigationService;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private readonly WindowEx _mainWindow;

    public DSCFileActivationHandler(
        ISetupFlowStringResource setupFlowStringResource,
        INavigationService navigationService,
        SetupFlowOrchestrator setupFlowOrchestrator,
        SetupFlowViewModel setupFlowViewModel,
        WindowEx mainWindow)
    {
        _setupFlowStringResource = setupFlowStringResource;
        _setupFlowViewModel = setupFlowViewModel;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        _navigationService = navigationService;
        _mainWindow = mainWindow;
    }

    protected override bool CanHandleInternal(FileActivatedEventArgs args)
    {
        return args.Files.Count > 0 && args.Files[0] is StorageFile file && file.FileType == WinGetFileExtension;
    }

    protected async override Task HandleInternalAsync(FileActivatedEventArgs args)
    {
        Debug.Assert(_navigationService.Frame != null, "Main window content is expected to be set before activation handlers are executed");
        var file = (StorageFile)args.Files[0];
        async void DSCActivationFlowHandlerAsync(object sender, RoutedEventArgs e)
        {
            // Only execute once
            _navigationService.Frame!.Loaded -= DSCActivationFlowHandlerAsync;
            await DSCActivationFlowAsync(file);
        }

        // If the application was activated from a file, the XamlRoot here is null
        if (_navigationService.Frame.XamlRoot == null)
        {
            // Wait until the frame is loaded before starting the flow
            _log.Information("DSC flow activated from a file but the application is not yet ready. Activation will start once the page is loaded.");
            _navigationService.Frame!.Loaded += DSCActivationFlowHandlerAsync;
        }
        else
        {
            // If the application was already running, start the flow immediately
            await DSCActivationFlowAsync(file);
        }
    }

    /// <summary>
    /// Navigates to the setup flow and starts the DSC activation flow
    /// </summary>
    /// <param name="file">The DSC file to activate</param>
    private async Task DSCActivationFlowAsync(StorageFile file)
    {
        try
        {
            // Don't interrupt the user if the machine configuration is in progress
            if (_setupFlowOrchestrator.IsMachineConfigurationInProgress)
            {
                _log.Warning("Cannot activate the DSC flow because the machine configuration is in progress");
                await _mainWindow.ShowErrorMessageDialogAsync(
                    _setupFlowStringResource.GetLocalized(StringResourceKey.ConfigurationViewTitle, file.Name),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.ConfigurationActivationFailedBusy),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.Close));
            }
            else
            {
                // Start the setup flow with the DSC file
                _log.Information("Starting DSC file activation");
                _navigationService.NavigateTo(typeof(SetupFlowViewModel).FullName!);
                await _setupFlowViewModel.StartFileActivationFlowAsync(file);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error executing the DSC activation flow");
        }
    }
}
