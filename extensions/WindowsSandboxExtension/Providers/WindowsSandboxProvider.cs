// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;

namespace WindowsSandboxExtension.Providers;

internal sealed class WindowsSandboxProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowsSandboxProvider));

    public string DisplayName => Constants.ProviderDisplayName;

    public Uri Icon => new(Constants.ExtensionIcon);

    public string Id => Constants.ProviderId;

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.None;

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind) => throw new NotImplementedException();

    public ICreateComputeSystemOperation CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson) => throw new NotImplementedException();

    public IAsyncOperation<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        return Task.Run(() =>
        {
            List<IComputeSystem> list = new();
            list.Add(new WindowsSandboxComputeSystem());

            return new ComputeSystemsResult(list);
        }).AsAsyncOperation();
    }
}
