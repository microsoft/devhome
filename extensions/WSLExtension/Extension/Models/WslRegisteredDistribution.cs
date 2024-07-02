// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Data;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WSLExtension.Contracts;
using WSLExtension.Helpers;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public delegate WslRegisteredDistribution WslRegisteredDistributionFactory(WslDistributionInfo distributionInfo);

public class WslRegisteredDistribution : IComputeSystem
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WslRegisteredDistribution));

    private readonly PackageHelper _packageHelper = new();

    private readonly string _versionLabel;

    private readonly string _defaultDistributionLabel;

    private readonly object _lock = new();

    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    private readonly WslDistributionInfo _distributionInfo;

    public IDeveloperId? AssociatedDeveloperId { get; set; }

    public string AssociatedProviderId { get; set; } = WslProviderId;

    public string DisplayName { get; set; }

    private ComputeSystemState _curState;

    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged;

    /// <summary>
    /// Gets or sets the Id of the compute system.All WSL distribution names are unique
    /// so we will use them as the Id for the compute system.
    /// </summary>
    public string Id { get; set; }

    public string SupplementalDisplayName { get; set; } = string.Empty;

    public ComputeSystemOperations SupportedOperations
    {
        get
        {
            if (GetState() == ComputeSystemState.Stopped)
            {
                return ComputeSystemOperations.Delete;
            }

            return ComputeSystemOperations.Delete | ComputeSystemOperations.Terminate;
        }
    }

    public WslRegisteredDistribution(
        IStringResource stringResource,
        WslDistributionInfo distributionInfo,
        IWslManager wslManager)
    {
        _stringResource = stringResource;
        _distributionInfo = distributionInfo;
        _wslManager = wslManager;
        DisplayName = distributionInfo.FriendlyName;

        if (!DisplayName.Equals(distributionInfo.Name, StringComparison.OrdinalIgnoreCase))
        {
            SupplementalDisplayName = distributionInfo.Name;
        }

        Id = distributionInfo.Name;
        _wslManager.DistributionStateSyncEventHandler += OnStateSyncRequested;
        _versionLabel = _stringResource.GetLocalized("WSLVersionLabel");
        _defaultDistributionLabel = _stringResource.GetLocalized("WSLDefaultDistributionLabel");
        _curState = _wslManager.IsDistributionRunning(Id) ? ComputeSystemState.Running : ComputeSystemState.Stopped;
    }

    private void OnStateSyncRequested(object? sender, HashSet<string> runningDistributions)
    {
        var newState = runningDistributions.Contains(Id)
            ? ComputeSystemState.Running
            : ComputeSystemState.Stopped;

        UpdateState(newState);
    }

    private void UpdateState(ComputeSystemState newState)
    {
        lock (_lock)
        {
            if (_curState != newState)
            {
                _curState = newState;
                StateChanged?.Invoke(this, newState);
            }
        }
    }

    private ComputeSystemState GetState()
    {
        lock (_lock)
        {
            return _curState;
        }
    }

    public void RemoveSubscriptions()
    {
        _wslManager.DistributionStateSyncEventHandler -= OnStateSyncRequested;
    }

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (GetState() == ComputeSystemState.Running)
                {
                    return new ComputeSystemStateResult(ComputeSystemState.Running);
                }

                return new ComputeSystemStateResult(ComputeSystemState.Stopped);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to get state for Distribution: {Id}");
                return new ComputeSystemStateResult(ex, ex.Message, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                UpdateState(ComputeSystemState.Stopping);
                _wslManager.TerminateDistribution(Id);
                UpdateState(ComputeSystemState.Stopped);
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to terminate all sessions for Distribution: {Id}");
                UpdateState(ComputeSystemState.Unknown);
                return GetErrorResult(ex, "WSLTerminateError");
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                UpdateState(ComputeSystemState.Deleting);
                _wslManager.UnregisterDistribution(Id);
                UpdateState(ComputeSystemState.Deleted);
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to unregister Distribution: {Id}");
                UpdateState(ComputeSystemState.Unknown);
                return GetErrorResult(ex, "WSLUnregisterError");
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrEmpty(_distributionInfo.Base64StringLogo))
                {
                    // No logo for this distribution so we'll use its PackageFamily logo
                    var packageFamilyLogo =
                        await _packageHelper.GetPackageIconAsByteArrayAsync(_distributionInfo.PackageFamilyName!);
                    return new ComputeSystemThumbnailResult(packageFamilyLogo);
                }

                var convertedArray = Convert.FromBase64String(_distributionInfo.Base64StringLogo!);
                return new ComputeSystemThumbnailResult(convertedArray);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to get thumbnail for Distribution: {Id}");
                return new ComputeSystemThumbnailResult(ex, ex.Message, ex.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                var properties = new List<ComputeSystemProperty>();
                if (_distributionInfo.Version2 != null)
                {
                    var versionNumber = _distributionInfo.Version2.Value ? 2 : 1;
                    properties.Add(ComputeSystemProperty.CreateCustom(versionNumber, _versionLabel, null));
                }

                if (_distributionInfo.IsDefaultDistribution)
                {
                    properties.Add(ComputeSystemProperty.CreateCustom(_defaultDistributionLabel, null, null));
                }

                return properties.AsEnumerable();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to get compute system properties for Distribution: {Id}");
                return new List<ComputeSystemProperty>();
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                _wslManager.LaunchDistribution(Id);
                UpdateState(ComputeSystemState.Running);
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to launch Distribution: {Id}");
                UpdateState(ComputeSystemState.Unknown);
                return GetErrorResult(ex, "WSLLaunchError");
            }
        }).AsAsyncOperation();
    }

    private ComputeSystemOperationResult GetUnSupportedResult()
    {
        var notImplementedException = new NotImplementedException($"Method not implemented by WSL Compute Systems");
        return new ComputeSystemOperationResult(notImplementedException, notImplementedException.Message, notImplementedException.Message);
    }

    public IApplyConfigurationOperation? CreateApplyConfigurationOperation(string configuration)
    {
        // Dev Home will handle null values as failed operations. We can't throw because this is an out of proc
        // COM call, so we'll lose the error information. We'll log the error and return null.
        return null;
    }

    private ComputeSystemOperationResult GetErrorResult(Exception ex, string resourceKey)
    {
        var displayMsg = _stringResource.GetLocalized(resourceKey, ex.Message);
        return new ComputeSystemOperationResult(ex, displayMsg, ex.Message);
    }

    // Unsupported IComputeSystem methods
    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options) =>
    Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options) =>
        Task.FromResult(GetUnSupportedResult()).AsAsyncOperation();
}
