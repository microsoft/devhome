// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WSLExtension.ClassExtensions;

namespace WSLExtension.Services;

[ComVisible(true)]
#if CANARY_BUILD
[Guid("EF2342AC-FF53-433D-9EDE-D395500F3B3E")]
#elif STABLE_BUILD
[Guid("121253AB-BA5D-4E73-99CF-25A2CB8BF173")]
#else
[Guid("7F572DC5-F40E-440F-B660-F579168B69B8")]
#endif
[ComDefaultInterface(typeof(IExtension))]
public sealed class WslExtension : IExtension, IDisposable
{
    private readonly IComputeSystemProvider _computeSystemProvider;

    private bool _disposed;

    public WslExtension(IComputeSystemProvider computeSystemProvider)
    {
        _computeSystemProvider = computeSystemProvider;
    }

    /// <summary>
    /// Gets the synchronization object that is used to prevent the main program from exiting
    /// until the extension is disposed.
    /// </summary>
    public ManualResetEvent ExtensionDisposedEvent { get; } = new(false);

    /// <summary>
    /// Gets provider object for the specified provider type.
    /// </summary>
    /// <param name="providerType">
    /// The provider type that the WSL extension may support. This is used to query the WSL
    /// extension for whether it supports the provider type.
    /// </param>
    /// <returns>
    /// When the extension supports the ProviderType the object returned will not be null. However,
    /// when the extension does not support the ProviderType the returned object will be null.
    /// </returns>
    public object? GetProvider(ProviderType providerType)
    {
        var log = Log.ForContext("SourceContext", nameof(WslExtension));
        object? provider = null;
        try
        {
            switch (providerType)
            {
                case ProviderType.ComputeSystem:
                    provider = _computeSystemProvider;
                    break;
                default:
                    log.Information($"Unsupported provider: {providerType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to get provider for provider type {providerType}");
        }

        return provider;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            ExtensionDisposedEvent.Set();
        }

        _disposed = true;
    }
}
