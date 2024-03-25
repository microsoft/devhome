// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the IComputeSystem interface that can be used throughout the application.
/// Note: Additional methods added to this class should be wrapped in try/catch blocks to ensure that
/// exceptions don't bubble up to the caller as the methods are cross proc COM calls.
/// </summary>
public class ComputeSystem
{
    private readonly string errorString;

    private readonly string _componentName = "ComputeSystem";

    private readonly IComputeSystem _computeSystem;

    public string? Id { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ComputeSystemOperations SupportedOperations { get; private set; }

    public string SupplementalDisplayName { get; private set; } = string.Empty;

    public IDeveloperId AssociatedDeveloperId { get; private set; }

    public string? AssociatedProviderId { get; private set; } = string.Empty;

    public ComputeSystem(IComputeSystem computeSystem)
    {
        _computeSystem = computeSystem;
        Id = new string(computeSystem.Id);
        DisplayName = new string(computeSystem.DisplayName);
        SupportedOperations = computeSystem.SupportedOperations;
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
            Log.Logger()?.ReportInfo(_componentName, $"Compute System State Changed for: {Id} to {state}");
            StateChanged(this, state);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"OnComputeSystemStateChanged for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"GetStateAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"StartAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"ShutDownAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"RestartAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"TerminateAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"DeleteAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"SaveAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"PauseAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"ResumeAsync for: {this} failed due to exception", ex);
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemOperationResult> ScheduleAsync(string options)
    {
        try
        {
            // placeholder until ComputerSystem.ScheduleAsync is implemented in the sdk
            return await _computeSystem.ShutDownAsync(options);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"ScheduleAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"CreateSnapshotAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"RevertSnapshotAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"DeleteSnapshotAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"ModifyPropertiesAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"GetComputeSystemThumbnailAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"GetComputeSystemPropertiesAsync for: {this} failed due to exception", ex);
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
            Log.Logger()?.ReportError(_componentName, $"ConnectAsync for: {this} failed due to exception", ex);
            return new ComputeSystemOperationResult(ex, errorString, ex.Message);
        }
    }

    public IApplyConfigurationOperation ApplyConfiguration(string configuration)
    {
        try
        {
            return _computeSystem.CreateApplyConfigurationOperation(configuration);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"ApplyConfiguration for: {this} failed due to exception", ex);
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
