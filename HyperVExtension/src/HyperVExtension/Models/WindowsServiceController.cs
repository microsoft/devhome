// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ServiceProcess;
using Serilog;

namespace HyperVExtension.Models;

/// <summary>
/// Class used as a wrapper for the ServiceController class which can be mocked.
/// </summary>
public class WindowsServiceController : IWindowsServiceController, IDisposable
{
    private readonly ServiceController _serviceController;

    public ServiceControllerStatus Status => _serviceController.Status;

    public string ServiceName
    {
        get => _serviceController.ServiceName;
        set => _serviceController.ServiceName = value;
    }

    public WindowsServiceController()
    {
        _serviceController = new();
    }

    public WindowsServiceController(string serviceName)
        : this()
    {
        _serviceController.ServiceName = serviceName;
    }

    private bool _disposed;

    public void ContinueService()
    {
        _serviceController.Continue();
    }

    public void StartService()
    {
        _serviceController.Start();
    }

    public void WaitForStatusChange(ServiceControllerStatus desiredStatus, TimeSpan timeout)
    {
        _serviceController.WaitForStatus(desiredStatus, timeout);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            var log = Log.ForContext("SourceContext", nameof(WindowsServiceController));
            log.Debug("Disposing WindowsServiceController");
            if (disposing)
            {
                _serviceController.Dispose();
            }
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
