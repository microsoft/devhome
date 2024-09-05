// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using COMRegistration;
using DevHome.Service.Runtime;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace DevHome.Service;

internal delegate void ServiceStopEvent();

internal sealed class ServiceLifetimeController
{
    private static readonly List<Process> _processes = new();

    internal static event ServiceStopEvent? ServiceStop;

    public static void RegisterProcess(Process p)
    {
        lock (_processes)
        {
            if (!_processes.Contains(p))
            {
                _processes.Add(p);
                p.EnableRaisingEvents = true;
                p.Exited += (sender, e) =>
                {
                    lock (_processes)
                    {
                        _processes.Remove(p);
                        if (_processes.Count == 0)
                        {
                            // It's ok to stop the service now
                            ServiceStop?.Invoke();
                            WindowsBackgroundService.Stop();
                        }
                    }
                };
            }
        }
    }

    public static bool CanUnload()
    {
        lock (_processes)
        {
            return _processes.Count == 0;
        }
    }
}
