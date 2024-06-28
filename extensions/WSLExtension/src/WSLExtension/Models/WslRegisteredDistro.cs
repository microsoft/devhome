// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.Common;
using WSLExtension.Exceptions;
using WSLExtension.Services;

namespace WSLExtension.Models;

public class WslRegisteredDistro : IComputeSystem
{
    private readonly IStringResource _stringResource;

    private readonly IWslManager _wslManager;

    public IDeveloperId AssociatedDeveloperId { get; set; } = null!;

    public string AssociatedProviderId { get; set; } = "Microsoft.WSL";

    public string DisplayName { get; set; } = null!;

    public string Id { get; set; } = null!;

    public string SupplementalDisplayName { get; set; } = string.Empty;

    public ComputeSystemOperations SupportedOperations =>
        ComputeSystemOperations.Delete;

    public bool? Running { get; set; }

    public bool? IsDefault { get; set; }

    public bool? IsWsl2 { get; set; }

    public string? Logo { get; set; }

    public string? WindowsTerminalProfileGuid { get; set; }

    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged = (s, e) => { };

    public WslRegisteredDistro(IStringResource stringResource, IWslManager wslManager)
    {
        _stringResource = stringResource;
        _wslManager = wslManager;
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
            if (Logo == null)
            {
                return new ComputeSystemThumbnailResult(null, string.Empty, string.Empty);
            }

            var uri = new Uri($"ms-appx:///WSLExtension/DistroDefinitions/Assets/{Logo}");
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
            var properties = new List<ComputeSystemProperty>();

            if (IsWsl2 != null)
            {
                properties.Add(ComputeSystemProperty.CreateCustom(IsWsl2.Value ? "2" : "1", "WSL", null));
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
