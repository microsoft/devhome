// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Exceptions;

public class HyperVManagerException : Exception
{
    public HyperVManagerException(string? message)
        : base(message)
    {
    }

    public HyperVManagerException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
