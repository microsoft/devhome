// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Represents the progress of a setup task.
/// </summary>
public sealed class TaskProgress
{
    public TaskProgress()
    {
    }

    public TaskProgress(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
