// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WindowsSandboxExtension.Helpers;

namespace WindowsSandboxExtension;

[ComVisible(true)]
[Guid("6A52115B-083C-4FB1-85F4-BBE23289220E")]
[ComDefaultInterface(typeof(IExtension))]
internal sealed class WindowsSandboxExtension : IExtension, IDisposable
{
    private readonly IHost _host;
    private readonly ILogger _logger;
    private bool _disposed;

    public WindowsSandboxExtension(IHost host)
    {
        _host = host;
        _logger = Log.ForContext("SourceContext", nameof(WindowsSandboxExtension));
    }

    public ManualResetEvent ExtensionDisposedEvent { get; } = new(false);

    public object? GetProvider(ProviderType providerType)
    {
        object? provider = null;

        try
        {
            switch (providerType)
            {
                case ProviderType.ComputeSystem:

                    provider = GetComputeSystemProvider();
                    break;
                default:
                    _logger.Information($"Unsupported provider: {providerType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Failed to get provider for provider type {providerType}");
        }

        return provider;
    }

    private object? GetComputeSystemProvider()
    {
        if (DependencyChecker.IsNewWindowsSandboxExtensionInstalled())
        {
            _logger.Information("New Windows Sandbox appx package is installed.");
            return null;
        }

        if (!DependencyChecker.IsOptionalComponentEnabled())
        {
            _logger.Information("Windows Sandbox optional component is not enabled.");
            return null;
        }

        return _host.Services.GetService(typeof(IComputeSystemProvider));
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
