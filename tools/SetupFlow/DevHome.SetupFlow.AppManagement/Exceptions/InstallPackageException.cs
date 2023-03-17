// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Exceptions;

/// <summary>
/// Exception thrown if package installation failed
/// </summary>
public class InstallPackageException : Exception
{
    private readonly InstallResultStatus _status;

    public InstallPackageException(InstallResultStatus status)
    {
        _status = status;
    }

    public InstallResultStatus Status => _status;
}
