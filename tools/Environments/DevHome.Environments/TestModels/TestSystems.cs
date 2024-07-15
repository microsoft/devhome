// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.Environments.Models;

public class TestSystems : IComputeSystem
{
    public TestSystems(string name, string thumbnailURI, string altName)
    {
        DisplayName = name;
        ThumbnailURI = thumbnailURI;
        SupplementalDisplayName = altName;
        ComputeSystemProperties = new List<ComputeSystemProperty>();
        ProcessProperties();
    }

    public string? Id { get; private set; }

    public string DisplayName { get; private set; }

    public string ThumbnailURI { get; private set; }

    public IEnumerable<ComputeSystemProperty> ComputeSystemProperties { get; set; }

    public string SupplementalDisplayName { get; set; }

    public IDeveloperId AssociatedDeveloperId => throw new NotImplementedException();

    public string AssociatedProviderId => throw new NotImplementedException();

    public event TypedEventHandler<IComputeSystem, ComputeSystemState>? StateChanged;

    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start | ComputeSystemOperations.ShutDown | ComputeSystemOperations.Restart | ComputeSystemOperations.CreateSnapshot | ComputeSystemOperations.RevertSnapshot | ComputeSystemOperations.Delete | ComputeSystemOperations.Pause | ComputeSystemOperations.Resume;

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync()
    {
        return Task.Run(async () =>
        {
            // ToDo: This is throwing; investigate, remove async/await
            // Learn why it is eating the exception
            await Task.Delay(10);
            return new ComputeSystemStateResult(ComputeSystemState.Running);
        }).AsAsyncOperation();
    }

    public void ProcessProperties()
    {
        var rand = new Random();
        var p1 = ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, rand.Next(1, 6));
        var p3 = ComputeSystemProperty.Create(ComputeSystemPropertyKind.StorageSizeInBytes, rand.Next(128, 512) * 1073741824L);
        var p4 = ComputeSystemProperty.Create(ComputeSystemPropertyKind.AssignedMemorySizeInBytes, rand.Next(8, 64) * 1073741824L);
        var properties = new List<ComputeSystemProperty>() { p1, p4, p3 };
        ComputeSystemProperties = properties;
    }

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options)
    {
        return Task.Run(async () =>
        {
            var uri = new Uri(ThumbnailURI);
            var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var randomAccessStream = await storageFile.OpenReadAsync();

            // Convert the stream to a byte array
            var bytes = new byte[randomAccessStream.Size];
            await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);

            return new ComputeSystemThumbnailResult(bytes);
        }).AsAsyncOperation();
    }

    private async Task TestStateChangesAsync()
    {
        StateChanged?.Invoke(this, ComputeSystemState.Stopping);
        await Task.Delay(2500);
        StateChanged?.Invoke(this, ComputeSystemState.Stopped);
        await Task.Delay(2500);
        StateChanged?.Invoke(this, ComputeSystemState.Starting);
        await Task.Delay(2500);
        StateChanged?.Invoke(this, ComputeSystemState.Running);
    }

    private IAsyncOperation<ComputeSystemOperationResult> TestOperation(string options)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(10);
            return new ComputeSystemOperationResult();
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options)
    {
        StateChanged?.Invoke(this, ComputeSystemState.Starting);
        return TestOperation(options);
    }

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options)
    {
        Task.Run(() => TestStateChangesAsync());
        return TestOperation(options);
    }

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options)
    {
        return Task.Run(async () =>
            {
                await Task.Delay(10);
                return ComputeSystemProperties;
            }).AsAsyncOperation();
    }

    // Unimplemented APIs
    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options) => throw new NotImplementedException();

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration) => throw new NotImplementedException();
}
