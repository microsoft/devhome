// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Configuration;

namespace HyperVExtension.DevSetupEngine;

public class PackageOperationException : Exception
{
    public enum ErrorCode
    {
        DevSetupErrorUpdateNotApplicable = unchecked((int)0x8500002B),
        DevSetupErrorMsStoreInstallFailed = unchecked((int)0x8500001E),
    }

    public PackageOperationException(ErrorCode errorCode, string message)
        : base(message)
    {
        HResult = (int)errorCode;
        Logging.Logger()?.ReportError(string.Empty, this);
    }
}
