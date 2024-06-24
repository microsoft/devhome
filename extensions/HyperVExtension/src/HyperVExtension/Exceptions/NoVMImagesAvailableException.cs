// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Common;

namespace HyperVExtension.Exceptions;

internal sealed class NoVMImagesAvailableException : Exception
{
    public NoVMImagesAvailableException(IStringResource stringResource)
    : base(stringResource.GetLocalized("NoImagesFoundError"))
    {
    }
}
