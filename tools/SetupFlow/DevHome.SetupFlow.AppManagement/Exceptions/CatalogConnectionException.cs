// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.Management.Deployment;

namespace DevHome.SetupFlow.AppManagement.Exceptions;

/// <summary>
/// Exception thrown if a catalog connection failed
/// </summary>
public class CatalogConnectionException : Exception
{
    private readonly ConnectResultStatus _status;

    public CatalogConnectionException(ConnectResultStatus status)
    {
        _status = status;
    }

    public ConnectResultStatus Status => _status;
}
