// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Exceptions;

public class WslServicesMediatorException : Exception
{
    public WslServicesMediatorException(string? message)
        : base(message)
    {
    }
}
