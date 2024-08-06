// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace FileExplorerGitIntegration.Models;

internal sealed class ThrottledTask
{
    private readonly TimeSpan _interval;
    private readonly object _lock = new();

    private readonly Action _action;

    private Task? _currentTask;
    private bool _shouldQueue;

    public ThrottledTask(Action action, TimeSpan interval)
    {
        _action = action;
        _interval = interval;
    }

    // When the action completes, wait for interval before checking if a new action has been queued.
    public void Run(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_currentTask != null && !_currentTask.IsCompleted)
            {
                _shouldQueue = true;
                return;
            }

            _currentTask = Task.Run(
                async () =>
                {
                    bool shouldContinue = true;
                    while (shouldContinue)
                    {
                        _action.Invoke();
                        await Task.Delay(_interval, cancellationToken);
                        lock (_lock)
                        {
                            if (_shouldQueue)
                            {
                                _shouldQueue = false;
                            }
                            else
                            {
                                shouldContinue = false;
                                _currentTask = null;
                            }
                        }
                    }
                },
                cancellationToken);
        }
    }
}
