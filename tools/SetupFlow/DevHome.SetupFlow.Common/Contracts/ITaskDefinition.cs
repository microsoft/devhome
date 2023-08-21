// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;
public interface ITaskDefinition
{
    public List<string> ToArgumentList();
}
