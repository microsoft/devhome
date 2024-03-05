// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.Activation;
using DevHome.Common.Services;
using DevHome.SetupFlow.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace DevHome.Services;

public class FileActivationHandler : ActivationHandler<FileActivatedEventArgs>
{
    private const string WinGetFileExtension = ".winget";
    private readonly INavigationService _navigationService;
    private readonly SetupFlowViewModel _setupFlowViewModel;

    public FileActivationHandler(INavigationService navigationService, SetupFlowViewModel setupFlowViewModel)
    {
        _setupFlowViewModel = setupFlowViewModel;
        _navigationService = navigationService;
    }

    protected override bool CanHandleInternal(FileActivatedEventArgs args)
    {
        return args.Files.Count > 0 && args.Files[0] is StorageFile file && file.FileType == WinGetFileExtension;
    }

    protected async override Task HandleInternalAsync(FileActivatedEventArgs args)
    {
        var file = args.Files[0] as StorageFile;
        _navigationService.NavigateTo(typeof(SetupFlowViewModel).FullName!);
        await _setupFlowViewModel.StartFileActivationFlowAsync(file);
    }
}
