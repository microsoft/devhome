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

    public TestComputeSystemProviderImpl(string displayName, string id)
    {
        DisplayName = displayName;
        Id = id;
    }

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.CreateComputeSystem;

    public Uri Icon => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSession(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSession(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId? developerId, string options)
    {
        return Task.Run(() =>
        {
            if (options == "Throw")
            {
                // simulate an extension letting an exception through
                throw new ArgumentNullException(nameof(options));
            }

            var computeSystemId = Guid.NewGuid().ToString();
            return new ComputeSystemsResult(new List<IComputeSystem> { new TestComputeSystemImpl(Id) });
        }).AsAsyncOperation();
    }

    public ICreateComputeSystemOperation CreateComputeSystem(IDeveloperId developerId, string options) => throw new NotImplementedException();
}
