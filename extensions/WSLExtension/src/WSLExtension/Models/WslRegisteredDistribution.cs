// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.Common;
using WSLExtension.Exceptions;
using WSLExtension.Extensions;
using WSLExtension.Helpers;
using WSLExtension.Services;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

public class WslRegisteredDistribution : IComputeSystem
{
    private readonly IStringResource _stringResource;

    private readonly PackageHelper _packageHelper = new();

    private readonly IWslManager _wslManager;

    private readonly DistributionState _distributionState;

    public IDeveloperId AssociatedDeveloperId { get; set; } = null!;

    public string AssociatedProviderId { get; set; } = "Microsoft.WSL";

    public string DisplayName { get; set; } = null!;

    public string Id { get; set; }

    public string SupplementalDisplayName { get; set; } = string.Empty;

    public ComputeSystemOperations SupportedOperations =>
        ComputeSystemOperations.Delete;

    public bool? Running { get; set; }

    public bool? IsDefault { get; set; }

    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged = (s, e) => { };

    public WslRegisteredDistribution(IStringResource stringResource, DistributionState distributionState, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _distributionState = distributionState;
        _wslManager = wslManager;
        DisplayName = distributionState.FriendlyName;
        SupplementalDisplayName = distributionState.DistributionName;
        Id = distributionState.DistributionName;
    }

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(() =>
        {
            if (Running != null && Running.Value)
            {
                return new ComputeSystemStateResult(ComputeSystemState.Running);
            }

            return new ComputeSystemStateResult(ComputeSystemState.Stopped);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options) => Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                StateChanged?.Invoke(this, ComputeSystemState.Deleting);
                Running = false;
                _wslManager.UnregisterDistribution(Id);
                StateChanged?.Invoke(this, ComputeSystemState.Deleted);
                return new ComputeSystemOperationResult();
            }
            catch (WslManagerException e)
            {
                StateChanged?.Invoke(this, ComputeSystemState.Unknown);
                return new ComputeSystemOperationResult(e, e.Message, e.Message);
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            try
            {
                // if there is a base64String for the logo of the distribution, use that. If not, default to using
                // the icon for the package family.
                if (!string.IsNullOrEmpty(_distributionState.Base64StringLogo))
                {
                    var convertedArray = Convert.FromBase64String(_distributionState.Base64StringLogo);
                    return new ComputeSystemThumbnailResult(convertedArray);
                }

                var byteArray = await _packageHelper.GetPackageIconAsByteArrayAsync(_distributionState.PackageFamilyName!);
                return new ComputeSystemThumbnailResult(byteArray);
            }
            catch (Exception)
            {
                return new ComputeSystemThumbnailResult(new InvalidDataException(), "error with thumbnail", "error with thumbnail");
            }
        }).AsAsyncOperation();
    }

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(() =>
        {
            var properties = new List<ComputeSystemProperty>();

            if (_distributionState.Version2 != null)
            {
                properties.Add(ComputeSystemProperty.CreateCustom(_distributionState.Version2.Value ? "2" : "1", "WSL", null));
            }

            if (IsDefault != null && IsDefault.Value)
            {
                properties.Add(ComputeSystemProperty.CreateCustom("Yes", "Default", null));
            }

            return properties.AsEnumerable();
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            StateChanged?.Invoke(this, ComputeSystemState.Starting);
            _wslManager.LaunchDistribution(Id);
            Running = true;
            StateChanged?.Invoke(this, ComputeSystemState.Running);
            return new ComputeSystemOperationResult();
        }).AsAsyncOperation();
    }

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration) =>
        throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options) =>
    Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options) =>
        Task.FromResult(new ComputeSystemOperationResult()).AsAsyncOperation();
}
