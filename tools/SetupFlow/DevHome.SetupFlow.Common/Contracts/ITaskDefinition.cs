// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Common.Contracts;
public abstract class TaskDefinition
{
    public abstract string ToCliArgument();

    public string EscapeValue(string value)
    {
        return value;
    }
}
