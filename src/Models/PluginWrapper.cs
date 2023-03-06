// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using DevHome.Common.Services;
using DevHome.Services;
using Microsoft.Windows.DevHome.SDK;
using WinRT;

namespace DevHome.Models;

public class PluginWrapper : IPluginWrapper
{
    private readonly string _classId;
    private readonly object _lock = new ();
    private readonly List<ProviderType> _providerTypes = new ();
    private IPlugin? _pluginObject;

    public PluginWrapper(string name, string classId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _classId = classId ?? throw new ArgumentNullException(nameof(classId));
    }

    public string Name
    {
        get;
    }

    public bool IsRunning()
    {
        // TODO : We also need to check if the underlying ptr is still alive
        // to make sure the other process is still running
        return _pluginObject is not null;
    }

    public async Task StartPlugin()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!IsRunning())
                {
                    IntPtr pluginPtr = IntPtr.Zero;
                    try
                    {
                        var hr = Ole32.CoCreateInstance(Guid.Parse(_classId), IntPtr.Zero, Ole32.CLSCTXLOCALSERVER, typeof(IPlugin).GUID, out pluginPtr);
                        if (hr < 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }

                        _pluginObject = MarshalInterface<IPlugin>.FromAbi(pluginPtr);
                    }
                    finally
                    {
                        if (pluginPtr != IntPtr.Zero)
                        {
                            Marshal.Release(pluginPtr);
                        }
                    }
                }
            }
        });
    }

    public void Kill()
    {
        lock (_lock)
        {
            if (IsRunning())
            {
                // TODO : Should we kill the process as well?
                _pluginObject = null;
            }
        }
    }

    public IPlugin? GetPluginObject()
    {
        lock (_lock)
        {
            if (IsRunning())
            {
                return _pluginObject;
            }
            else
            {
                return null;
            }
        }
    }

    public void AddProviderType(ProviderType providerType)
    {
        _providerTypes.Add(providerType);
    }

    public bool HasProviderType(ProviderType providerType)
    {
        return _providerTypes.Contains(providerType);
    }
}
