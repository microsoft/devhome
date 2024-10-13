// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Environments.Exceptions;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the IComputeSystemProvider interface that can be used throughout the application.
/// Note: Additional methods added to this class should be wrapped in try/catch blocks to ensure that
/// exceptions don't bubble up to the caller as the methods are cross proc COM calls.
/// </summary>
public class ComputeSystemProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ComputeSystemProvider));

    private readonly string _errorString;

    private readonly IComputeSystemProvider _computeSystemProvider;

    public string Id { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ComputeSystemProviderOperations SupportedOperations { get; private set; }

    public Uri Icon { get; }

    public ComputeSystemProvider(IComputeSystemProvider computeSystemProvider)
    {
        _computeSystemProvider = computeSystemProvider;
        Id = computeSystemProvider.Id;
        DisplayName = computeSystemProvider.DisplayName;
        SupportedOperations = computeSystemProvider.SupportedOperations;
        Icon = computeSystemProvider.Icon;
        _errorString = StringResourceHelper.GetResource("ComputeSystemUnexpectedError", DisplayName);
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        try
        {
            return _computeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerId, sessionKind);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"CreateAdaptiveCardSessionForDeveloperId for: {this} failed due to exception");
            return new ComputeSystemAdaptiveCardResult(ex, _errorString, ex.Message);
        }
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForComputeSystem(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind)
    {
        try
        {
            return _computeSystemProvider.CreateAdaptiveCardSessionForComputeSystem(computeSystem, sessionKind);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"CreateAdaptiveCardSessionForComputeSystem for: {this} failed due to exception");
            return new ComputeSystemAdaptiveCardResult(ex, _errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId, CancellationToken cancellationToken)
    {
        try
        {
            return await _computeSystemProvider.GetComputeSystemsAsync(developerId).AsTask(cancellationToken);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetComputeSystemsAsync for: {this} failed due to exception");
            return new ComputeSystemsResult(ex, _errorString, ex.Message);
        }
    }

    public ICreateComputeSystemOperation? CreateCreateComputeSystemOperation(IDeveloperId developerId, string inputJson)
    {
        try
        {
            return _computeSystemProvider.CreateCreateComputeSystemOperation(developerId, inputJson)
                ?? throw new CreateCreateComputeSystemOperationException("CreateCreateComputeSystemOperation was null");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"GetComputeSystemsAsync for: {this} failed due to exception");
            return new FailedCreateComputeSystemOperation(ex, StringResourceHelper.GetResource("CreationOperationStoppedUnexpectedly"));
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem provider ID: {Id} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem provider display name: {DisplayName} ");

        var supportedOperations = EnumHelper.SupportedOperationsToString<ComputeSystemProviderOperations>(SupportedOperations);
        builder.AppendLine(CultureInfo.InvariantCulture, $"ComputeSystem provider supported operations : {string.Join(", ", supportedOperations)} ");
        return builder.ToString();
    }
}
