// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Common.Models;
using Windows.Foundation;

namespace DevHome.SetupFlow.AppManagement.Models;

internal class InstallPackageTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    // As we don't have this information available for each package in the WinGet COM API,
    // simply assume that any package installation may need a reboot.
    public bool RequiresReboot => true;

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    public LoadingMessages GetLoadingMessages() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();
}
