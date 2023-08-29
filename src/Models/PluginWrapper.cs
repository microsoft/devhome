// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace DevHome.Models;

public class ExtensionWrapper : IExtensionWrapper
{
    private const int HResultRpcServerNotRunning = -2147023174;

    private readonly object _lock = new ();
    private readonly List<ProviderType> _providerTypes = new ();

    private readonly Dictionary<Type, ProviderType> _providerTypeMap = new ()
    {
        [typeof(IDeveloperIdProvider)] = ProviderType.DeveloperId,
        [typeof(IRepositoryProvider)] = ProviderType.Repository,
        [typeof(INotificationsProvider)] = ProviderType.Notifications,
        [typeof(IWidgetProvider)] = ProviderType.Widget,
        [typeof(ISettingsProvider)] = ProviderType.Settings,
        [typeof(IDevDoctorProvider)] = ProviderType.DevDoctor,
        [typeof(ISetupFlowProvider)] = ProviderType.SetupFlow,
    };

    private IExtension? _pluginObject;

    public ExtensionWrapper(string name, string packageFullName, string classId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        PackageFullName = packageFullName ?? throw new ArgumentNullException(nameof(packageFullName));
        ExtensionClassId = classId ?? throw new ArgumentNullException(nameof(classId));
    }

    public string Name
    {
        get;
    }

    public string PackageFullName
    {
        get;
    }

    public string ExtensionClassId
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

    public async Task StartExtensionAsync()
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                if (!IsRunning())
                {
                    var pluginPtr = IntPtr.Zero;
                    try
                    {
                        var hr = PInvoke.CoCreateInstance(Guid.Parse(ExtensionClassId), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IExtension).GUID, out var pluginObj);
                        pluginPtr = Marshal.GetIUnknownForObject(pluginObj);
                        if (hr < 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }

                        _pluginObject = MarshalInterface<IExtension>.FromAbi(pluginPtr);
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

    public IExtension? GetExtensionObject()
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
        await StartExtensionAsync();

        return GetExtensionObject()?.GetProvider(_providerTypeMap[typeof(T)]) as T;
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
