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
    private const int HResultRpcServerNotRunning = -2147023174;

    private readonly object _lock = new ();
    private readonly List<ProviderType> _providerTypes = new ();

    private readonly Dictionary<Type, ProviderType> _providerTypeMap = new ()
    {
        [typeof(IDevIdProvider)] = ProviderType.DevId,
        [typeof(IRepositoryProvider)] = ProviderType.Repository,
        [typeof(INotificationsProvider)] = ProviderType.Notifications,
        [typeof(IWidgetProvider)] = ProviderType.Widget,
        [typeof(ISettingsProvider)] = ProviderType.Settings,
        [typeof(IDevDoctorProvider)] = ProviderType.DevDoctor,
        [typeof(ISetupFlowProvider)] = ProviderType.SetupFlow,
    };

    private IPlugin? _pluginObject;

    public PluginWrapper(string name, string classId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PluginClassId = classId ?? throw new ArgumentNullException(nameof(classId));
    }

    public string Name
    {
        get;
    }

    public string PluginClassId
    {
        get;
    }

    public bool IsRunning()
    {
        if (_pluginObject is null)
        {
            return false;
        }

        try
        {
            _pluginObject.As<IInspectable>().GetRuntimeClassName();
        }
        catch (COMException e)
        {
            if (e.ErrorCode == HResultRpcServerNotRunning)
            {
                return false;
            }

            throw;
        }

        return true;
    }

    public async Task StartPluginAsync()
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
                        var hr = Ole32.CoCreateInstance(Guid.Parse(PluginClassId), IntPtr.Zero, Ole32.CLSCTXLOCALSERVER, typeof(IPlugin).GUID, out pluginPtr);
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

    public void SignalDispose()
    {
        lock (_lock)
        {
            if (IsRunning())
            {
                _pluginObject?.Dispose();
            }

            _pluginObject = null;
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

    public async Task<T?> GetProviderAsync<T>()
        where T : class
    {
        await StartPluginAsync();

        return GetPluginObject()?.GetProvider(_providerTypeMap[typeof(T)]) as T;
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

public class Ole32
{
    // https://docs.microsoft.com/windows/win32/api/wtypesbase/ne-wtypesbase-clsctx
    public const int CLSCTXLOCALSERVER = 0x4;

    // https://docs.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cocreateinstance
    [DllImport(nameof(Ole32))]

#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern int CoCreateInstance(
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        out IntPtr ppv);
#pragma warning restore CA1401 // P/Invokes should not be visible
}
