// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Exceptions;

public class WslManagerException : Exception
{
    public WslManagerException(string? message)
        : base(message)
    {
    }

    public WslManagerException(string? message, Exception? innerException)
    : base(message, innerException)
    {
    }
}
