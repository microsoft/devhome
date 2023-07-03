// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Contract.TaskOperator;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.TaskOperator;
public class TaskOperatorFactory : ITaskOperatorFactory
{
    private readonly IWindowsPackageManager _wpm;

    public TaskOperatorFactory(IWindowsPackageManager wpm)
    {
        _wpm = wpm;
    }

    public void WriteToStdOut(string message) => Console.WriteLine(message);

    public IConfigurationOperator CreateConfigurationOperator() => new ConfigurationOperator();

    public IDevDriveStorageOperator CreateDevDriveStorageOperator() => new DevDriveStorageOperator();

    public IInstallOperator CreateInstallOperator() => new InstallOperator(_wpm);
}
