// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.ViewModels;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace DevHome.Services;
internal class FileActivationHandler : ActivationHandler<FileActivatedEventArgs>
{
    protected override bool CanHandleInternal(FileActivatedEventArgs args)
    {
        return args.Files.Count > 0 && args.Files[0] is StorageFile file && file.FileType == ".devhome";
    }

    protected async override Task HandleInternalAsync(FileActivatedEventArgs args)
    {
        var setupFlowViewModel = Application.Current.GetService<SetupFlowViewModel>();
        var navigationService = Application.Current.GetService<INavigationService>();
        var file = args.Files[0] as StorageFile;
        navigationService.NavigateTo(typeof(DevHome.SetupFlow.ViewModels.SetupFlowViewModel).FullName!);
        await setupFlowViewModel.StartFileActivationFlow(file);
        await Task.CompletedTask;
    }
}
