// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;

namespace HyperVExtension.Exceptions;

internal sealed class DownloadOperationFailedException : Exception
{
    public DownloadOperationFailedException(IStringResource stringResource)
        : base(stringResource.GetLocalized("DownloadOperationFailed"))
    {
    }

    public DownloadOperationFailedException(string message)
        : base(message)
    {
    }
}
