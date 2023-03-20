// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Common.Models;
using Windows.Foundation;

namespace DevHome.SetupFlow.ConfigurationFile.Models;

internal class ConfigureTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    public bool RequiresReboot => throw new NotImplementedException();

    public bool DependsOnDevDriveToBeInstalled
    {
        get; set;
    }

    public ActionCenterMessages GetErrorMessages() => throw new NotImplementedException();

    public TaskMessages GetLoadingMessages() => throw new NotImplementedException();

    public ActionCenterMessages GetNeedsAttentionMessages() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();
}
