// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WSLExtension.Common.Extensions;

namespace WSLExtension;

[ComVisible(true)]
[Guid("121253AB-BA5D-4E73-99CF-25A2CB8BF173")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class WslExtension : IExtension, IDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    public WslExtension(IHost host)
    {
        _host = host;
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
    /// The provider type that the Hyper-V extension may support. This is used to query the Hyper-V
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
                    provider = _host.GetService<IComputeSystemProvider>();
                    break;
                default:
                    log.Information($"Unsupported provider: {providerType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to get provider for provider type {providerType}", ex);
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
