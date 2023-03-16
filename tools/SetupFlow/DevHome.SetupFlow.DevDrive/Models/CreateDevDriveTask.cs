// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Common.Models;
using Windows.Foundation;

namespace DevHome.SetupFlow.DevDrive.Models;

internal class CreateDevDriveTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public ActionCenterErrorMessages GetActionCenterMessages() => throw new NotImplementedException();

    public TaskMessages GetLoadingMessages() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();
}
