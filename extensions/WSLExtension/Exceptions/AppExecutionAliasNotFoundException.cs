// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Exceptions;

public class AppExecutionAliasNotFoundException : Exception
{
    public AppExecutionAliasNotFoundException(string? message)
        : base(message)
    {
    }
}
