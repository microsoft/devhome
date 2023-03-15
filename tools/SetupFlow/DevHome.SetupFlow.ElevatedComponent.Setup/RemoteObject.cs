// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent;

public class RemoteObject<T> : IDisposable
{
    private readonly Mutex _completionMutex;
    private bool _disposedValue;

    public T Value { get; }

    public RemoteObject(T value, Mutex completionMutex)
    {
        Value = value;
        _completionMutex = completionMutex;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _completionMutex.ReleaseMutex();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
