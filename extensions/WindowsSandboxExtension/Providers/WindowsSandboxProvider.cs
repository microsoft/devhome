// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation;
using WindowsSandboxExtension.Helpers;

namespace WindowsSandboxExtension.Providers;

internal sealed class WindowsSandboxProvider : IComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WindowsSandboxProvider));

    public string DisplayName => Constants.ProviderDisplayName;

    public Uri Icon => new(Constants.ExtensionIcon);

    public string Id => Constants.ProviderId;

    public ComputeSystemProviderOperations SupportedOperations => ComputeSystemProviderOperations.None;

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind)
    {
        return new ComputeSystemAdaptiveCardResult(
            new NotImplementedException(),
            Resources.GetResource("NotImplemented", _log),
            "Create Windows Sandbox compute system is not implemented.");
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        return new ComputeSystemAdaptiveCardResult(
            new NotImplementedException(),
            Resources.GetResource("NotImplemented", _log),
            "Developer Id Adaptive Card session is not implmented for Windows Sandbox.");
    }

    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson)
    {
        return null;
    }

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
