// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;

namespace HyperVExtension.Exceptions;

internal sealed class DownloadOperationCancelledException : Exception
{
    public DownloadOperationCancelledException(IStringResource stringResource)
        : base(stringResource.GetLocalized("DownloadOperationCancelled"))
    {
    }
}
