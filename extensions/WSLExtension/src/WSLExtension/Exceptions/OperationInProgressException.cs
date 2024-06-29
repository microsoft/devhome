// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WSLExtension.Common;

namespace WSLExtension.Exceptions;

internal sealed class OperationInProgressException : Exception
{
    public OperationInProgressException(IStringResource stringResource)
        : base(stringResource.GetLocalized("OperationInProgressError"))
    {
    }
}
