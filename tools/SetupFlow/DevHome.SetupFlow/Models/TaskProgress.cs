// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models;

public class TaskProgress
{
    public TaskProgress(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
