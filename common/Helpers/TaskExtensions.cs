// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Helpers;
public static class TaskExtensions
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, int timeoutMs)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs));

        if (completedTask.IsCanceled)
        {
            throw new TaskCanceledException();
        }

        if (completedTask.IsFaulted)
        {
            throw completedTask.Exception!;
        }

        if (completedTask != task)
        {
            throw new TimeoutException("Task did not execute in time.");
        }

        return task.GetAwaiter().GetResult();
    }
}
