// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.UI.Converters;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Helpers;
using DevHome.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;
using Log = DevHome.Common.Helpers.Log;

namespace DevHome.Models;

public sealed class PluginWrapper : IPluginWrapper
{
    private const int HResultRpcServerNotRunning = -2147023174;

    private readonly object _lock = new ();

    private readonly Dictionary<Type, object> _providerTypeToObjectMap = new ();
    private readonly IReadOnlyDictionary<Type, string> _providerTypeToClassIdMap;

    public PluginWrapper(
        string id,
        string name,
        string? description,
        StorageFolder? publicFolder,
        string packageFullName,
        IReadOnlyDictionary<Type, string> providerToClassIdMap)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        PackageFullName = packageFullName ?? throw new ArgumentNullException(nameof(providerToClassIdMap));
        PublicFolder = publicFolder;
        _providerTypeToClassIdMap = providerToClassIdMap ?? throw new ArgumentNullException(nameof(providerToClassIdMap));
    }

    public string Id
    {
        get;
    }

    public string Name
    {
        get;
    }

    public string PackageFullName
    {
        get;
    }

    public string? Description
    {
        get;
    }

    public StorageFolder? PublicFolder
    {
        get;
    }

    public async Task<T?> GetProviderAsync<T>(bool invalidateCache = false)
        where T : class
    {
        if (!HasProvider<T>())
        {
            return await Task.FromResult(default(T));
        }

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (invalidateCache)
                {
                    _providerTypeToObjectMap.Remove(typeof(T));
                }
                else if (_providerTypeToObjectMap.TryGetValue(typeof(T), out var providerType))
                {
                    return providerType as T;
                }

                IntPtr pluginPtr = IntPtr.Zero;
                try
                {
                    var guid = Guid.Parse(_providerTypeToClassIdMap[typeof(T)]);
                    var hr = PInvoke.CoCreateInstance(guid, null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(T).GUID, out var pluginObj);
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    if (pluginObj is null)
                    {
                        return null;
                    }

                    var provider = MarshalInterface<T>.FromAbi(Marshal.GetIUnknownForObject(pluginObj));
                    if (provider is null)
                    {
                        return null;
                    }

                    _providerTypeToObjectMap.Add(typeof(T), provider);
                    return provider;
                }
                finally
                {
                    if (pluginPtr != IntPtr.Zero)
                    {
                        // There are 3 addrefs happening in total
                        // First in CoCreateInstance
                        // Then in Marshal.GetIUnknown
                        // Then in MarshalInterface.FromAbi
                        // Thus we do a release twice so we have exactly once reference to the underlying com object.
                        Marshal.Release(pluginPtr);
                        Marshal.Release(pluginPtr);
                    }
                }
            }
        });
    }

    public bool HasProvider<T>()
    {
        return _providerTypeToClassIdMap.ContainsKey(typeof(T));
    }

    public async Task<ExtensionQueryResult<TResult>> QueryAsync<TProvider, TResult>(Func<TProvider, Task<TResult>> handler, int timeoutMs = 200)
        where TProvider : class
    {
        try
        {
            var provider = await GetProviderAsync<TProvider>().WithTimeout(2000);
            if (provider is null)
            {
                throw new ExtensionException($"Invalid provider {typeof(TProvider)} for extension {Id}: {Name}");
            }

            var result = await handler.Invoke(provider).WithTimeout(timeoutMs);
            return new ExtensionQueryResult<TResult>(true, result, null);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError($"Exception occured while calling an api in {Id}: {Name}", e);
            return new ExtensionQueryResult<TResult>(false, default, e);
        }
    }

    public void Dispose()
    {
        if (_providerTypeToObjectMap.TryGetValue(typeof(IDeveloperIdProvider), out var devIdProvider))
        {
            (devIdProvider as IDeveloperIdProvider)!.Dispose();
        }

        if (_providerTypeToObjectMap.TryGetValue(typeof(IRepositoryProvider), out var repoProvider))
        {
            (repoProvider as IRepositoryProvider)!.Dispose();
        }

        _providerTypeToObjectMap.Clear();
    }
}
