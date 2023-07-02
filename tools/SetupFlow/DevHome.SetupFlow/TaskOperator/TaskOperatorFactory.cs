// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Contract.TaskOperator;
using Windows.Foundation;
using Windows.Storage;

namespace DevHome.SetupFlow.TaskOperator;
public class TaskOperatorFactory : ITaskOperatorFactory
{
    public IConfigurationOperator CreateConfigurationOperator() => new ConfigurationOperator();

    public IDevDriveStorageOperator CreateDevDriveStorageOperator() => new DevDriveStorageOperator();

    public IInstallOperator CreateInstallOperator() => new InstallOperator();
}

public class DevDriveResult : IDevDriveResult
{
    public Exception HResult { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}

public class ConfigurationUnitResult : IConfigurationUnitResult
{
    public Exception HResult { get; set; }

    public string Intent { get; set; }

    public bool IsSkipped { get; set; }

    public string UnitName { get; set; }
}

public class ApplyConfigurationResult : IApplyConfigurationResult
{
    public IList<IConfigurationUnitResult> UnitResults { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}

public class InstallPackageResult : IInstallPackageResult
{
    public int ExtendedErrorCode { get; set; }

    public uint InstallerErrorCode { get; set; }

    public int Status { get; set; }

    public bool Attempted { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}

public class DevDriveStorageOperator : IDevDriveStorageOperator
{
    public IDevDriveResult CreateDevDrive(string virtDiskPath, ulong sizeInBytes, char newDriveLetter, string driveLabel)
    {
        return new DevDriveResult();
    }
}

public class ConfigurationOperator : IConfigurationOperator
{
    public IAsyncOperation<IApplyConfigurationResult> ApplyConfigurationAsync(StorageFile file)
    {
        return Task.Run(() =>
        {
            IApplyConfigurationResult result = new ApplyConfigurationResult();
            return result;
        }).AsAsyncOperation();
    }
}

public class InstallOperator : IInstallOperator
{
    public IInstallPackageResult InstallPackageAsync(string packageId, string catalogName)
    {
        return new InstallPackageResult()
        {
            Attempted = true,
        };
    }
}
