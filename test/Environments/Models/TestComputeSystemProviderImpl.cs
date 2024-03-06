// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Test.Environments.Models;

/// <summary>
/// Test class that implements IComputeSystemProvider.
/// </summary>
public class TestComputeSystemProviderImpl : IComputeSystemProvider
{
    public string DisplayName { get; set; }

    public string Id { get; set; }

    public TestComputeSystemProviderImpl(string displayName, string id, bool shouldThrow)
    {
        DisplayName = displayName;
        Id = id;
        ShouldThrow = shouldThrow;
    }

    public bool ShouldThrow { get; set; }

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public Uri Icon => new("ms-resource://text.png");

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId? developerId)
    {
        return Task.Run(() =>
        {
            if (ShouldThrow)
            {
                // simulate an extension letting an exception through
                throw new TimeoutException("throwing timeout as a test");
            }

            var computeSystemId = Guid.NewGuid().ToString();
            return new ComputeSystemsResult(new List<IComputeSystem> { new TestComputeSystemImpl(Id) });
        }).AsAsyncOperation();
    }

    public ICreateComputeSystemOperation CreateComputeSystem(IDeveloperId developerId, string options) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson) => throw new NotImplementedException();
}
