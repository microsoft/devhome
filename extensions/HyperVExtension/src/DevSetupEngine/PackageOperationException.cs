// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Serilog;

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
        var log = Log.ForContext("SourceContext", nameof(PackageOperationException));
        log.Error(this, message);
    }
}
