// Copyright (c) Microsoft Corporation.
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
using WindowsSandboxExtension.Helpers;
using static System.Runtime.InteropServices.JavaScript.JSType;

#nullable disable
namespace WindowsSandboxExtension.Providers;

public class WindowsSandboxComputeSystem : IComputeSystem
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowsSandboxProvider));

    public string AssociatedProviderId => Constants.ProviderId;

    public string DisplayName => Resources.GetResource("WindowsSandboxDisplayName", _log);

    public string Id => Guid.NewGuid().ToString();

    public string SupplementalDisplayName => string.Empty;

    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.None;

    public IDeveloperId AssociatedDeveloperId { get; set; }

#pragma warning disable CS0067
    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged;
#pragma warning restore CS0067

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
                ComputeSystemProperty.Create(ComputeSystemPropertyKind.CpuCount, 4),
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
            return new ComputeSystemStateResult(ComputeSystemState.Unknown);
        }).AsAsyncOperation();
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options)
    {
        return Task.Run(() =>
        {
            try
            {
                var systemRoot = Environment.GetEnvironmentVariable("SYSTEMROOT");
                Process.Start(Path.Join(systemRoot, "System32\\WindowsSandbox.exe"));
                return new ComputeSystemOperationResult();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to start Windows Sandbox");
                return new ComputeSystemOperationResult(ex, Resources.GetResource("WindowsSandboxFailedToStart", _log), "Failed to start Windows Sandbox");
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

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options) => throw new NotImplementedException();
}
