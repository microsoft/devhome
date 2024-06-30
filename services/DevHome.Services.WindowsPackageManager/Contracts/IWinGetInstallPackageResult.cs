// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.WindowsPackageManager.Contracts;

public interface IWinGetInstallPackageResult
{
    /// <summary>
    /// Gets a value indicating whether a restart is required to complete the
    /// installation
    /// </summary>
    public bool RebootRequired { get; }

    /// <summary>
    ///  Gets the error code of the overall operation.
    /// </summary>
    /// <remarks>
    /// Reference: https://github.com/msftrubengu/winget-cli/blob/demo/src/Microsoft.Management.Deployment/PackageManager.idl
    /// </remarks>
    public int ExtendedErrorCode { get; }
}
