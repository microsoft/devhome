// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Environments.Models;

public class TestSystemProvider : IComputeSystemProvider
{
    public string DisplayName => "Test Hyper-V";

    public string Id => "DevHome.Test.IComputeSystemProvider";

    public IEnumerable<IComputeSystem> GetComputeSystems(IDeveloperId developerId)
    {
        var computeSystems = new List<IComputeSystem>
        {
            new TestSystems("Phi", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Beta"),
            new TestSystems("Epsilon", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Alpha"),
            new TestSystems("Gamma", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Beta"),
        };
        return computeSystems;
    }

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(10);
            var computeSystems = GetComputeSystems(developerId);
            return new ComputeSystemsResult(computeSystems);
        }).AsAsyncOperation();
    }

    // Unimplemented APIs
    public ComputeSystemProviderOperations SupportedOperations => throw new NotImplementedException();

    public Uri Icon => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateComputeSystem(IDeveloperId developerId, string options) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson) => throw new NotImplementedException();
}
