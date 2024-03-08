﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Logging;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace DevHome.Services;

public class DSCActivationHandler : ActivationHandler<FileActivatedEventArgs>
{
    private const string WinGetFileExtension = ".winget";
    private readonly INavigationService _navigationService;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private readonly WindowEx _mainWindow;

    public DSCActivationHandler(
        INavigationService navigationService,
        SetupFlowOrchestrator setupFlowOrchestrator,
        SetupFlowViewModel setupFlowViewModel,
        WindowEx mainWindow)
    {
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
            _navigationService.Frame!.Loaded += DSCActivationFlowHandlerAsync;
        }
        else
        {
            await DSCActivationFlowAsync(file);
        }
    }

    private async Task DSCActivationFlowAsync(StorageFile file)
    {
        try
        {
            if (_setupFlowOrchestrator.FlowPages.Count > 1)
            {
                await _mainWindow.ShowErrorMessageDialogAsync(
                    $"{file.Name}",
                    "Cannot activate the DSC flow because the machine configuration is in progress",
                    "Close");
            }
            else
            {
                // Start the setup flow with the DSC file
                _navigationService.NavigateTo(typeof(SetupFlowViewModel).FullName!);
                await _setupFlowViewModel.StartFileActivationFlowAsync(file);
            }
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError("Error executing the DSC activation flow", ex);
        }
    }
}
