// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVManagerException : Exception
{
    public HyperVManagerException(string? message, int hresult = 0)
        : base(message)
    {
        HResult = hresult;
    }

    public HyperVManagerException(string? message, Exception? innerException)
    : base(message, innerException)
    {
        HResult = innerException?.HResult ?? 0;
    }
}
