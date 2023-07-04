// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.Contract.TaskOperator;

namespace DevHome.SetupFlow.TaskOperator;

public class InstallPackageResult : IInstallPackageResult
{
    public int ExtendedErrorCode { get; set; }

    public uint InstallerErrorCode { get; set; }

    public int Status { get; set; }

    public bool RebootRequired { get; set; }

    public bool Succeeded { get; set; }
}
