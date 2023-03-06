// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Common.Models;
using Windows.Foundation;

namespace DevHome.SetupFlow.DevVolume.Models;

internal class CreateDevVolumeTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    public bool RequiresReboot => false;

    public LoadingMessages GetLoadingMessages() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();
}
