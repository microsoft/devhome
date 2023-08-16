// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class InstallTaskDefinition : ITaskDefinition
{
    public Guid TaskId
    {
        get; set;
    }

    public string PackageId
    {
        get; set;
    }

    public string CatalogName
    {
        get; set;
    }
}
