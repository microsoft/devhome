// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DevHome.Common.Models;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.DevDrive.Models;

internal class CreateDevDriveTask : ISetupTask
{
    private readonly TaskMessages _taskMessages;
    private readonly ActionCenterMessages _actionCenterMessages = new ();

    public bool RequiresAdmin => true;

    public bool RequiresReboot => false;

    public bool DependsOnDevDriveToBeInstalled => false;

    public IDevDrive DevDrive
    {
        get; set;
    }

    public CreateDevDriveTask(IDevDrive devDrive)
    {
        DevDrive = devDrive;
        _taskMessages = new TaskMessages("Creating Dev Drive...", "Dev Drive created", "Couldn’t create Dev Drive", "Created Dev Drive (restart required)");
        _actionCenterMessages.PrimaryMessage = "Couldn’t create Dev Drive";
    }

    public ActionCenterMessages GetErrorMessages() => new ();

    public TaskMessages GetLoadingMessages() => _taskMessages;

    public ActionCenterMessages GetRebootMessage() => new ();

    IAsyncOperation<TaskFinishedState> ISetupTask.Execute()
    {
        return Task.Run(() =>
        {
            return TaskFinishedState.Failure;
        }).AsAsyncOperation();
    }

    IAsyncOperation<TaskFinishedState> ISetupTask.ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory) => throw new NotImplementedException();
}
