// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Common.Contracts;

public sealed class ConfigurationTaskDefinition : ITaskDefinition
{
    public Guid TaskId
    {
        get; set;
    }

    public string Content
    {
        get; set;
    }
}
