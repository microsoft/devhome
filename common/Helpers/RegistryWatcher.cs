// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace DevHome.Common;

[Flags]
public enum RegistryNotifyFilterFlags
{
    SubKeyAddRemove = 0x1,
    AttributesChange = 0x2,
    ValueChanged = 0x4,
    SecurityChanged = 0x8,
}

public delegate void RegistryChangedEventHandler();

/// <summary>
/// Registry watcher class.
/// Utilizes RegNotifyChangeKeyValue Win32 API to watch for registry changes.
/// Calls RegistryChangedEventHandler delegate when registry change is detected.
/// </summary>
public sealed class RegistryWatcher : IDisposable
{
    private readonly Serilog.ILogger _log = Log.ForContext("SourceContext", nameof(RegistryWatcher));

    public event RegistryChangedEventHandler RegistryChanged;

    private readonly AutoResetEvent _waitEvent;
    private readonly RegistryKey? _key;
    private readonly REG_NOTIFY_FILTER _notifyFilter;
    private bool _started;
    private bool _disposed;

    public RegistryWatcher(RegistryKey key, string keyPath, RegistryChangedEventHandler callback, RegistryNotifyFilterFlags filter = RegistryNotifyFilterFlags.ValueChanged)
    {
        _notifyFilter = TranslateFlags(filter);
        _waitEvent = new AutoResetEvent(true);

        // TODO: Verify with the one other caller, that there wasn't a negative dependency around creating the key unintentionally or having write access.
        _key = key.OpenSubKey(keyPath, false);
        _key ??= key.CreateSubKey(keyPath);
        if (_key == null)
        {
            _log.Error($"Cannot open {keyPath} registry key. Error: {Marshal.GetLastWin32Error()}");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        RegistryChanged += callback;

        _log.Information("Registry Watcher created.");
    }

    private REG_NOTIFY_FILTER TranslateFlags(RegistryNotifyFilterFlags filter)
    {
        REG_NOTIFY_FILTER result = REG_NOTIFY_FILTER.REG_NOTIFY_THREAD_AGNOSTIC;

        if (filter.HasFlag(RegistryNotifyFilterFlags.SubKeyAddRemove))
        {
            result |= REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_NAME;
        }
        else if (filter.HasFlag(RegistryNotifyFilterFlags.AttributesChange))
        {
            result |= REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_ATTRIBUTES;
        }
        else if (filter.HasFlag(RegistryNotifyFilterFlags.ValueChanged))
        {
            result |= REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET;
        }
        else if (filter.HasFlag(RegistryNotifyFilterFlags.SecurityChanged))
        {
            result |= REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_SECURITY;
        }

        return result;
    }

    public void Start()
    {
        lock (this)
        {
            if (!_started)
            {
                _started = true;
                _waitEvent.Reset();
                Task.Run(() =>
                {
                    try
                    {
                        while (_started)
                        {
                            var result = PInvoke.RegNotifyChangeKeyValue(_key!.Handle, true, _notifyFilter, _waitEvent.SafeWaitHandle, true);
                            if (result != WIN32_ERROR.ERROR_SUCCESS)
                            {
                                throw new Win32Exception((int)result);
                            }

                            _waitEvent.WaitOne();

                            if (_started)
                            {
                                try
                                {
                                    RegistryChanged();
                                }
                                catch (Exception ex)
                                {
                                    _log.Error(ex, "RegistryChanged delegate failed.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Registry Watcher thread failed.");
                    }
                });
                _log.Information("Registry Watcher thread started.");
            }
        }
    }

    public void Stop()
    {
        lock (this)
        {
            if (_started)
            {
                _started = false;
                _waitEvent.Set();
                _log.Information("Registry Watcher thread stopped.");
            }
        }
    }

    public void WaitForRegistryChange()
    {
        _log.Information("Waiting for registry change.");
        _waitEvent.WaitOne();
        _log.Information("Registry change detected.");
        RegistryChanged?.Invoke();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _key?.Dispose();
                _waitEvent.Dispose();
            }

            _disposed = true;
        }
    }
}
