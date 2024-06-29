// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.Services.WindowsPackageManager.Exceptions;

/// <summary>
/// Exception thrown if a find package operation failed
/// </summary>
public class FindPackagesException : Exception
{
    private readonly FindPackagesResultStatus _status;

    public FindPackagesException(FindPackagesResultStatus status)
    {
        _status = status;
    }

    public FindPackagesResultStatus Status => _status;
}
