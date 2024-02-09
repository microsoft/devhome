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
    public string DefaultComputeSystemProperties
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public string DisplayName => "Test Hyper-V";

    public string Id => "DevHome.Test.IComputeSystemProvider";

    public string Properties => throw new NotImplementedException();

    public IEnumerable<IComputeSystem> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        var computeSystems = new List<IComputeSystem>
        {
            new TestSystems("Phi", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Beta"),
            new TestSystems("Epsilon", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Alpha"),
            new TestSystems("Gamma", "ms-appx:///Assets/Preview/AppList.scale-200.png", "Project Beta"),
        };
        return computeSystems;
    }

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId, string options)
    {
        return Task.Run(async () =>
        {
            await Task.Delay(10);
            var computeSystems = GetComputeSystemsAsync(developerId);
            return new ComputeSystemsResult(computeSystems);
        }).AsAsyncOperation();
    }

    // Unimplemented APIs
    public ComputeSystemProviderOperations SupportedOperations => throw new NotImplementedException();

    public Uri Icon => throw new NotImplementedException();

    public IAsyncOperationWithProgress<CreateComputeSystemResult, ComputeSystemOperationData> CreateComputeSystemAsync(IDeveloperId developerId, string options) => throw new NotImplementedException();

    ComputeSystemAdaptiveCardResult IComputeSystemProvider.CreateAdaptiveCardSession(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    ComputeSystemAdaptiveCardResult IComputeSystemProvider.CreateAdaptiveCardSession(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateComputeSystem(IDeveloperId developerId, string options) => throw new NotImplementedException();
}
