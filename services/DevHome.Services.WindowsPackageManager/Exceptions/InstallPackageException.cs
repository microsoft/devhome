// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Exceptions;

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

    /// <summary>
    ///  Gets or sets the error code of the overall operation.
    /// </summary>
    /// <remarks>
    /// Reference: https://github.com/msftrubengu/winget-cli/blob/demo/src/Microsoft.Management.Deployment/PackageManager.idl
    /// </remarks>
    public int ExtendedErrorCode { get; set; }

    public InstallResultStatus Status { get; }

    // Install error codes:
    // Reference: https://github.com/microsoft/winget-cli/blob/master/src/AppInstallerSharedLib/Public/AppInstallerErrors.h
    public const int InstallErrorPackageInUse = unchecked((int)0x8A150101);
    public const int InstallErrorInstallInProgress = unchecked((int)0x8A150102);
    public const int InstallErrorFileInUse = unchecked((int)0x8A150103);
    public const int InstallErrorMissingDependency = unchecked((int)0x8A150104);
    public const int InstallErrorDiskFull = unchecked((int)0x8A150105);
    public const int InstallErrorInsufficientMemory = unchecked((int)0x8A150106);
    public const int InstallErrorNoNetwork = unchecked((int)0x8A150107);
    public const int InstallErrorContactSupport = unchecked((int)0x8A150108);
    public const int InstallErrorRebootRequiredToFinish = unchecked((int)0x8A150109);
    public const int InstallErrorRebootRequiredToInstall = unchecked((int)0x8A15010A);
    public const int InstallErrorRebootInitiated = unchecked((int)0x8A15010B);
    public const int InstallErrorCancelledByUser = unchecked((int)0x8A15010C);
    public const int InstallErrorAlreadyInstalled = unchecked((int)0x8A15010D);
    public const int InstallErrorDowngrade = unchecked((int)0x8A15010E);
    public const int InstallErrorBlockedByPolicy = unchecked((int)0x8A15010F);
    public const int InstallErrorDependencies = unchecked((int)0x8A150110);
    public const int InstallErrorPackageInUseByApplication = unchecked((int)0x8A150111);
    public const int InstallErrorInvalidParameter = unchecked((int)0x8A150112);
    public const int InstallErrorSystemNotSupported = unchecked((int)0x8A150113);

    public InstallPackageException(InstallResultStatus status, int extendedErrorCode, uint installerErrorCode)
    {
        Status = status;
        InstallerErrorCode = installerErrorCode;
        ExtendedErrorCode = extendedErrorCode;
    }
}
