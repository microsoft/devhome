// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Exceptions;

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
