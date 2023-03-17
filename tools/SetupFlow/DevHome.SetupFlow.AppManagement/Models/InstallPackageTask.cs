// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Models;
using Windows.Foundation;

namespace DevHome.SetupFlow.AppManagement.Models;

public class InstallPackageTask : ISetupTask
{
    private readonly IWindowsPackageManager _wpm;
    private readonly WinGetPackage _package;

    public bool RequiresAdmin => false;

    // As we don't have this information available for each package in the WinGet COM API,
    // simply assume that any package installation may need a reboot.
    public bool RequiresReboot => true;

    public LoadingMessages GetLoadingMessages()
    {
        return new LoadingMessages
        {
            Executing = "Installing ",
            Error = "Error installing ",
            Finished = "Installed ",
        };
    }

    public InstallPackageTask(IWindowsPackageManager wpm, WinGetPackage package)
    {
        _wpm = wpm;
        _package = package;
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(async () =>
        {
            try
            {
                await _package.InstallAsync(_wpm);
                return TaskFinishedState.Success;
            }
            catch
            {
                return TaskFinishedState.Failure;
            }
        }).AsAsyncOperation();
    }
}
