// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Wrapper class for the IComputeSystemProvider interface that can be used throughout the application.
/// Note: Additional methods added to this class should be wrapped in try/catch blocks to ensure that
/// exceptions don't bubble up to the caller as the methods are cross proc COM calls.
/// </summary>
public class ComputeSystemProvider
{
    private readonly string errorString;

    private readonly string _componentName = "ComputeSystemProvider";

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
        errorString = StringResourceHelper.GetResource("ComputeSystemUnexpectedError", DisplayName);
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSessionForDeveloperId(IDeveloperId developerId, ComputeSystemAdaptiveCardKind sessionKind)
    {
        try
        {
            return _computeSystemProvider.CreateAdaptiveCardSessionForDeveloperId(developerId, sessionKind);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"CreateAdaptiveCardSessionWithDeveloperId for: {this} failed due to exception", ex);
            return new ComputeSystemAdaptiveCardResult(ex, errorString, ex.Message);
        }
    }

    public ComputeSystemAdaptiveCardResult CreateAdaptiveCardSession(IComputeSystem computeSystem, ComputeSystemAdaptiveCardKind sessionKind)
    {
        try
        {
            return _computeSystemProvider.CreateAdaptiveCardSessionForComputeSystem(computeSystem, sessionKind);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"CreateAdaptiveCardSessionWithComputeSystem for: {this} failed due to exception", ex);
            return new ComputeSystemAdaptiveCardResult(ex, errorString, ex.Message);
        }
    }

    public async Task<ComputeSystemsResult> GetComputeSystemsAsync(IDeveloperId developerId)
    {
        try
        {
            return await _computeSystemProvider.GetComputeSystemsAsync(developerId);
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError(_componentName, $"GetComputeSystemsAsync for: {this} failed due to exception", ex);
            return new ComputeSystemsResult(ex, errorString, ex.Message);
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
