// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using DevHome.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Windows.Devices.Geolocation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace HyperVExtension.DevSetupAgent;

internal delegate void RegistryChangedEventHandler();

/// <summary>
/// Registry watcher class.
/// Utilizes RegNotifyChangeKeyValue Win32 API to watch for registry changes.
/// Calls RegistryChangedEventHandler delegate when registry change is detected.
/// </summary>
internal sealed class RegistryWatcher : IDisposable
{
    public event RegistryChangedEventHandler RegistryChanged;

    private readonly AutoResetEvent _waitEvent;
    private readonly RegistryKey? _key;
    private bool _started;
    private bool _disposed;

    public RegistryWatcher(RegistryKey key, string keyPath, RegistryChangedEventHandler callback)
    {
        _waitEvent = new AutoResetEvent(true);
        _key = key.CreateSubKey(keyPath);
        if (_key == null)
        {
            Logging.Logger()?.ReportError($"Cannot open {keyPath} registry key. Error: {Marshal.GetLastWin32Error()}");
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        RegistryChanged += callback;

        Logging.Logger()?.ReportInfo("Registry Watcher created.");
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
                            var notifyFilter = REG_NOTIFY_FILTER.REG_NOTIFY_CHANGE_LAST_SET |
                                               REG_NOTIFY_FILTER.REG_NOTIFY_THREAD_AGNOSTIC;
                            var result = PInvoke.RegNotifyChangeKeyValue(_key!.Handle, true, notifyFilter, _waitEvent.SafeWaitHandle, true);
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
                                    Logging.Logger()?.ReportError("RegistryChanged delegate failed.", ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger()?.ReportError("Registry Watcher thread failed.", ex);
                    }
                });
                Logging.Logger()?.ReportInfo("Registry Watcher thread started.");
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
                Logging.Logger()?.ReportInfo("Registry Watcher thread stopped.");
            }
        }
    }

    public void WaitForRegistryChange()
    {
        Logging.Logger()?.ReportInfo("Waiting for registry change.");
        _waitEvent.WaitOne();
        Logging.Logger()?.ReportInfo("Registry change detected.");
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
