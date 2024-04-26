// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace WindowsSandboxExtension;

[ComVisible(true)]
[Guid("696e7e4c-2f09-45af-b6d6-610a1b8f0da7")]
[ComDefaultInterface(typeof(IExtension))]
internal sealed class WindowsSandboxExtension : IExtension, IDisposable
{
    private readonly IHost _host;
    private bool _disposed;

    public WindowsSandboxExtension(IHost host)
    {
        _host = host;
    }

    public ManualResetEvent ExtensionDisposedEvent { get; } = new(false);

    public object? GetProvider(ProviderType providerType)
    {
        var log = Log.ForContext("SourceContext", nameof(WindowsSandboxExtension));
        object? provider = null;

        try
        {
            switch (providerType)
            {
                case ProviderType.ComputeSystem:
                    provider = _host.Services.GetService(typeof(IComputeSystemProvider));
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
        if (!_disposed)
        {
            if (disposing)
            {
                ExtensionDisposedEvent.Set();
            }

            _disposed = true;
        }
    }
}
