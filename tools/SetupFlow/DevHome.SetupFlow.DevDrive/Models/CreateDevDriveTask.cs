// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Windows.Input;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.DevDrive.Models;

internal class CreateDevDriveTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    public ActionCenterMessages GetErrorMessages() => throw new NotImplementedException();

    public TaskMessages GetLoadingMessages() => throw new NotImplementedException();

    public ActionCenterMessages GetRebootMessage() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
