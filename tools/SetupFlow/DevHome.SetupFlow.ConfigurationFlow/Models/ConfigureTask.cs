// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.ConfigurationFile.Models;

internal class ConfigureTask : ISetupTask
{
    public bool RequiresAdmin => throw new NotImplementedException();

    public bool RequiresReboot => throw new NotImplementedException();

    public LoadingMessages GetLoadingMessages() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute() => throw new NotImplementedException();

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
