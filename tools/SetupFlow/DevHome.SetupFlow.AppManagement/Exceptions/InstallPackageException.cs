// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Exceptions;

/// <summary>
/// Exception thrown if package installation failed
/// </summary>
public class InstallPackageException : Exception
{
    private readonly InstallResultStatus _status;

    /// <summary>
    /// The error code from the install attempt. Only valid if the Status is
    /// <see cref="InstallResultStatus.InstallError"/> This value's meaning
    /// will require knowledge of the specific installer or install technology.
    /// </summary>
    private readonly uint _installerErrorCode;

    public InstallPackageException(InstallResultStatus status, uint installerErrorCode)
    {
        _status = status;
        _installerErrorCode = installerErrorCode;
    }

    public InstallResultStatus Status => _status;

    public uint InstallerErrorCode => _installerErrorCode;
}
