// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Test.Environments.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Test.Environments.Models;

/// <summary>
/// Test class that implements IComputeSystem.
/// </summary>
public class TestComputeSystemImpl : IComputeSystem
{
    public string SupplementalDisplayName { get; set; }

    public IDeveloperId AssociatedDeveloperId => new TestDeveloperId();

    public string AssociatedProviderId { get; set; }

    public string Id { get; set; }

    public string DisplayName { get; set; }

    public ComputeSystemOperations SupportedOperations => ComputeSystemOperations.Start |
                ComputeSystemOperations.ShutDown |
                ComputeSystemOperations.Terminate |
                ComputeSystemOperations.Delete |
                ComputeSystemOperations.Save |
                ComputeSystemOperations.Pause |
                ComputeSystemOperations.Resume |
                ComputeSystemOperations.Restart |
                ComputeSystemOperations.CreateSnapshot |
                ComputeSystemOperations.RevertSnapshot |
                ComputeSystemOperations.DeleteSnapshot |
                ComputeSystemOperations.ModifyProperties |
                ComputeSystemOperations.ApplyConfiguration;

    public event TypedEventHandler<IComputeSystem, ComputeSystemState> StateChanged = (s, e) => { };

    public TestComputeSystemImpl(string providerId)
    {
        Id = TestHelpers.ComputeSystemId;
        DisplayName = TestHelpers.ComputeSystemName;
        AssociatedProviderId = providerId;
        SupplementalDisplayName = TestHelpers.ComputeSystemAlternativeDisplayName;
    }

    public IAsyncOperation<ComputeSystemOperationResult> ConnectAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> CreateSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> DeleteSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<IEnumerable<ComputeSystemProperty>> GetComputeSystemPropertiesAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemThumbnailResult> GetComputeSystemThumbnailAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemStateResult> GetStateAsync() => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ModifyPropertiesAsync(string inputJson) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> PauseAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RestartAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ResumeAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> RevertSnapshotAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> SaveAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> ShutDownAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> StartAsync(string options) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemOperationResult> TerminateAsync(string options) => throw new NotImplementedException();

    public IApplyConfigurationOperation CreateApplyConfigurationOperation(string configuration) => throw new NotImplementedException();
}
