// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace FileExplorerGitIntegration.Models;

internal sealed class ThrottledTask
{
    private readonly TimeSpan _interval;
    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch = new();

    private readonly Action _action;

    private Task? _currentTask;
    private bool _running;

    private bool Throttled => _stopwatch.Elapsed < _interval;

    public ThrottledTask(Action action, TimeSpan interval)
    {
        _action = action;
        _interval = interval;
    }

    public void Run(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_currentTask != null && (_running || Throttled))
            {
                return;
            }

            _running = true;
            _currentTask = Task.Run(_action, cancellationToken);
            _currentTask.ContinueWith(
                task =>
            {
                _stopwatch.Restart();
                _running = false;
            },
                cancellationToken);

            return;
        }
    }
}
