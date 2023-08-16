// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Common.Contracts;
public interface ITaskDefinition
{
    public Guid TaskId
    {
        get;
    }
}
