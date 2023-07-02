// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Contract.TaskOperator;

namespace DevHome.SetupFlow.TaskOperator;
public class TaskOperatorFactory : ITaskOperatorFactory
{
    public void WriteToStdOut(string message) => Console.WriteLine(message);

    public IConfigurationOperator CreateConfigurationOperator() => new ConfigurationOperator();

    public IDevDriveStorageOperator CreateDevDriveStorageOperator() => new DevDriveStorageOperator();

    public IInstallOperator CreateInstallOperator() => new InstallOperator();
}
