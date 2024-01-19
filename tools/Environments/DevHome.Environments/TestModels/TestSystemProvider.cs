// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Environments.Models;

internal class TestSystemProvider : IComputeSystemProvider
{
    public string DefaultComputeSystemProperties
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public string DisplayName => "Test Hyper-V";

    public string Id => "DevHome.Test.IComputeSystemProvider";

    public string Properties => throw new NotImplementedException();

    public ExtensionIcon Icon => throw new NotImplementedException();

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
    ComputeSystemProviderOperations IComputeSystemProvider.SupportedOperations => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemAdaptiveCardResult> CreateAdaptiveCardSession(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemAdaptiveCardResult> CreateAdaptiveCardSession(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public IAsyncOperation<CreateComputeSystemResult> CreateComputeSystemAsync(string options) => throw new NotImplementedException();

    public IAsyncOperationWithProgress<CreateComputeSystemResult, ComputeSystemOperationData> CreateComputeSystemAsync(IDeveloperId developerId, string options) => throw new NotImplementedException();
}
