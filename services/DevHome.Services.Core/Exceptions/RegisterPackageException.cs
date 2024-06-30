// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Services.Core.Exceptions;

/// <summary>
/// Exception thrown if a package registration failed
/// </summary>
public class RegisterPackageException : Exception
{
    public RegisterPackageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
