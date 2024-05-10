// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Win32;
using WinRT;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the IComputeSystem interface that can be used throughout the application.
/// Note: Additional methods added to this class should be wrapped in try/catch blocks to ensure that
/// exceptions don't bubble up to the caller as the methods are cross proc COM calls.
/// </summary>
public class ComputeSystem
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystem));

    private readonly string errorString;

    private readonly IComputeSystem _computeSystem;

    public string? Id { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ComputeSystemOperations SupportedOperations
    {
        get
        {
            try
            {
                return _computeSystem.SupportedOperations;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to get supported operations for {DisplayName}");
                return ComputeSystemOperations.None;
            }
        }
    }

    public string SupplementalDisplayName { get; private set; } = string.Empty;

    public IDeveloperId AssociatedDeveloperId { get; private set; }

    public string AssociatedProviderId { get; private set; } = string.Empty;

    public ComputeSystem(IComputeSystem computeSystem)
    {
        _computeSystem = computeSystem;
        Id = new string(computeSystem.Id);
        DisplayName = new string(computeSystem.DisplayName);
        SupplementalDisplayName = new string(computeSystem.SupplementalDisplayName);
        AssociatedDeveloperId = computeSystem.AssociatedDeveloperId;
        AssociatedProviderId = new string(computeSystem.AssociatedProviderId);
        _computeSystem.StateChanged += OnComputeSystemStateChanged;
        errorString = StringResourceHelper.GetResource("ComputeSystemUnexpectedError", DisplayName);
    }

    public event TypedEventHandler<ComputeSystem, ComputeSystemState> StateChanged = (sender, state) => { };

    public void OnComputeSystemStateChanged(object? sender, ComputeSystemState state)
    {
        try
        {
            _log.Information($"Compute System State Changed for: {Id} to {state}");
            StateChanged(this, state);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"OnComputeSystemStateChanged for: {this} failed due to exception");
        }
    }

    public async Task<ComputeSystemStateResult> GetStateAsync()
    {
        try
        {
            return await _computeSystem.GetStateAsync();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetStateAsync for: {this} failed due to exception");
            return new ComputeSystemStateResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> StartAsync(string options)
    {
        try
        {
            return await _computeSystem.StartAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"StartAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> ShutDownAsync(string options)
    {
        try
        {
            return await _computeSystem.ShutDownAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"ShutDownAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> RestartAsync(string options)
    {
        try
        {
            return await _computeSystem.RestartAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"RestartAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        try
        {
            return await _computeSystem.TerminateAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"TerminateAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        try
        {
            return await _computeSystem.DeleteAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"DeleteAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> SaveAsync(string options)
    {
        try
        {
            return await _computeSystem.SaveAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"SaveAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> PauseAsync(string options)
    {
        try
        {
            return await _computeSystem.PauseAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"PauseAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> ResumeAsync(string options)
    {
        try
        {
            return await _computeSystem.ResumeAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"ResumeAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> CreateSnapshotAsync(string options)
    {
        try
        {
            return await _computeSystem.CreateSnapshotAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"CreateSnapshotAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> RevertSnapshotAsync(string options)
    {
        try
        {
            return await _computeSystem.RevertSnapshotAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"RevertSnapshotAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> DeleteSnapshotAsync(string options)
    {
        try
        {
            return await _computeSystem.DeleteSnapshotAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"DeleteSnapshotAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> ModifyPropertiesAsync(string options)
    {
        try
        {
            return await _computeSystem.ModifyPropertiesAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"ModifyPropertiesAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        try
        {
            return await _computeSystem.GetComputeSystemThumbnailAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetComputeSystemThumbnailAsync for: {this} failed due to exception");
            return new ComputeSystemThumbnailResult(ex, errorString, ex.Message);
        }
    }

    public async Task<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        try
        {
            return await _computeSystem.GetComputeSystemPropertiesAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetComputeSystemPropertiesAsync for: {this} failed due to exception");
            return new List<ComputeSystemProperty>();
        }
    }

    public async Task<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        try
        {
            return await _computeSystem.ConnectAsync(options);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"ConnectAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    // We need to give DevHomeAzureExtension the ability to SetForeground on the processes it creates. In some cases
    // these processes need to show UI, in some cases they call APIs that only succeed if they are called from a
    // foreground process. We call CoAllowSetForegroundWindow on the COM interface that we are about to use to allow
    // the process to set foreground window.
    // CoAllowSetForegroundWindow must be called on a raw COM interface, not a .NET CCW, in order to work correctly, since
    // the underlying functionality is implemented by COM runtime and the object itself. CoAllowSetForegroundWindow wrapper
    // below takes a WinRT object and extracts the raw COM interface pointer from it before calling native CoAllowSetForegroundWindow.
    [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
    private static extern void CoAllowSetForegroundWindow(IntPtr pUnk, IntPtr lpvReserved);

    private void CoAllowSetForegroundWindow(IComputeSystem2 computeSystem2)
    {
        CoAllowSetForegroundWindow(((IWinRTObject)computeSystem2).NativeObject.ThisPtr, 0);
    }

    public async Task<ComputeSystemOperationResult> PinToStartMenuAsync(string options)
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.PinToStartMenuAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"PinToStartMenuAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> UnpinFromStartMenuAsync(string options)
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.UnpinFromStartMenuAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"UnpinFromStartMenuAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> PinToTaskbarAsync(string options)
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.PinToTaskbarAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"PinToTaskbarAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> UnpinFromTaskbarAsync(string options)
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.UnpinFromTaskbarAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"UnpinFromTaskbarAsync for: {this} failed due to exception");
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemPinnedResult> GetIsPinnedToStartMenuAsync()
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.GetIsPinnedToStartMenuAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetIsPinnedToStartMenuAsync for: {this} failed due to exception");
            return new ComputeSystemPinnedResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemPinnedResult> GetIsPinnedToTaskbarAsync()
    {
        try
        {
            if (_computeSystem is IComputeSystem2 computeSystem2)
            {
                CoAllowSetForegroundWindow(computeSystem2);
                return await computeSystem2.GetIsPinnedToTaskbarAsync();
            }

            throw new InvalidOperationException();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetIsPinnedToTaskbarAsync for: {this} failed due to exception");
            return new ComputeSystemPinnedResult(ex, errorString, ex.Message);
        }
    }

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration)
    {
        try
        {
            return _computeSystem.CreateApplyConfigurationOperation(configuration);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"CreateApplyConfigurationOperation for: {this} failed due to exception");
            throw;
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem ID: {Id} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem name: {DisplayName} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem SupplementalDisplayName: {SupplementalDisplayName} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated Provider Id : {AssociatedProviderId} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated developerId LoginId: {AssociatedDeveloperId?.LoginId} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem associated developerId Url: {AssociatedDeveloperId?.Url} ");

        var supportedOperations = EnumHelper.SupportedOperationsToString<ComputeSystemOperations>(SupportedOperations);
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem supported operations : {string.Join(",", supportedOperations)} ");

        return builder.ToString();
    }
}
