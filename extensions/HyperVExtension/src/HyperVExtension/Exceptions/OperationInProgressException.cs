// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;

namespace HyperVExtension.Exceptions;

internal sealed class OperationInProgressException : Exception
{
    public OperationInProgressException(IStringResource stringResource)
        : base(stringResource.GetLocalized("OperationInProgressError"))
    {
    }
}
