// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.Exceptions;

/// <summary>
/// Exception thrown if package installation failed
/// </summary>
public class InstallPackageException : Exception
{
    /// <summary>
    /// Gets the error code from the install attempt. Only valid if the Status is
    /// <see cref="InstallResultStatus.InstallError"/> This value's meaning
    /// will require knowledge of the specific installer or install technology.
    /// </summary>
    /// <remarks>
    /// Reference: https://github.com/msftrubengu/winget-cli/blob/demo/src/Microsoft.Management.Deployment/PackageManager.idl
    /// </remarks>
    public uint InstallerErrorCode { get; }

    public InstallResultStatus Status { get; }

    public InstallPackageException(InstallResultStatus status, uint installerErrorCode)
    {
        Status = status;
        InstallerErrorCode = installerErrorCode;
    }
}
