﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using WindowsSandboxExtension.Helpers;
using WindowsSandboxExtension.Telemetry;

using Timer = System.Timers.Timer;

namespace WindowsSandboxExtension.Providers;

public class WindowsSandboxComputeSystem : IComputeSystem, IDisposable
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowsSandboxProvider));
    private readonly object _windowsSandboxStartLock = new();

    private Process? _windowsSandboxExeProcess;
    private ComputeSystemState _state = ComputeSystemState.Stopped;

    private ComputeSystemState State
    {
        get => _state;

        set
        {
            _state = value;
            StateChanged?.Invoke(this, value);
        }
    }

    public string AssociatedProviderId => Constants.ProviderId;

    public string DisplayName => Resources.GetResource("WindowsSandboxDisplayName", _log);

    public string Id => _id.ToString();

    public string SupplementalDisplayName => string.Empty;

    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Terminate;

    public IDeveloperId? AssociatedDeveloperId => null;

    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged;

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            var uri = new Uri(Constants.Thumbnail);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);
            return new ComputeSystemThumbnailResult(bytes);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(() =>
        {
            var properties = new List<ComputeSystemProperty>
            {
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, Environment.ProcessorCount),
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.AssignedMemorySizeInBytes, 4294967296),
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.StorageSizeInBytes, 85899345920),
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.UptimeIn100ns, 100),
            };

            return properties.AsEnumerable();
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(() =>
        {
            return new ComputeSystemStateResult(State);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                lock (_windowsSandboxStartLock)
                {
                    // Windows Sandbox is not running.
                    if (_windowsSandboxExeProcess == null || _windowsSandboxExeProcess.HasExited)
                    {
                        var system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        var windowsSandboxExePath = Path.Combine(system32Path, Constants.WindowsSandboxExe);

                        _windowsSandboxExeProcess = new();
                        _windowsSandboxExeProcess.StartInfo.FileName = windowsSandboxExePath;
                        _windowsSandboxExeProcess.EnableRaisingEvents = true;
                        _windowsSandboxExeProcess.Exited += WindowsSandboxProcessExited;

                        Thread.Sleep((int)TimeSpan.FromSeconds(30).TotalMilliseconds);

                        State = ComputeSystemState.Running;
                        TraceLogging.StartingWindowsSandbox();

                        _windowsSandboxExeProcess.Start();

                        PInvoke.SetForegroundWindow((HWND)_windowsSandboxExeProcess.MainWindowHandle);
                    }

                    BringWindowsSandboxClientToForeground();

                    return new ComputeSystemOperationResult();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to start Windows Sandbox");
                TraceLogging.ExceptionThrown(ex);
                return new ComputeSystemOperationResult(ex, Resources.GetResource("WindowsSandboxFailedToStart", _log), "Failed to start Windows Sandbox");
            }
        }).AsAsyncOperation();
    }

    private void WindowsSandboxProcessExited(object? sender, EventArgs e)
    {
        State = ComputeSystemState.Stopped;
        _windowsSandboxExeProcess?.Dispose();
        _windowsSandboxExeProcess = null;
    }

    private Process? WindowsSandboxClientProcess()
    {
        return Process.GetProcessesByName("WindowsSandboxClient").FirstOrDefault();
    }

    private void BringWindowsSandboxClientToForeground()
    {
        var clientProcess = WindowsSandboxClientProcess();
        var windowHandle = clientProcess?.MainWindowHandle ?? IntPtr.Zero;

        PInvoke.SetForegroundWindow((HWND)windowHandle);
    }

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(() =>
        {
            lock (_windowsSandboxStartLock)
            {
                if (_windowsSandboxExeProcess != null)
                {
                    _windowsSandboxExeProcess.CloseMainWindow();
                    WindowsSandboxClientProcess()?.CloseMainWindow();
                }

                return new ComputeSystemOperationResult();
            }
        }).AsAsyncOperation();
    }

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options) => throw new NotImplementedException();

    public void Dispose()
    {
        _windowsSandboxExeProcess?.Dispose();
        GC.SuppressFinalize(this);
    }
}
