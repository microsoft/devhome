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

    public IConfigurationOperator CreateConfigurationOperator() => new ConfigurationOperator();

    public IDevDriveStorageOperator CreateDevDriveStorageOperator() => new DevDriveStorageOperator();

    public IInstallPackageOperator CreateInstallPackageOperator() => new InstallOperator(_wpm);

    public ITestOperator CreateTestOperator() => new TestOperator();
}
