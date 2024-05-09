// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Storage;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Wrapper class for the IQuickStartProjectProvider interface that can be used throughout the application.
/// Note: Additional methods added to this class should be wrapped in try/catch blocks to ensure that
/// exceptions don't bubble up to the caller as the methods are cross proc COM calls.
/// </summary>
public sealed class QuickStartProjectProvider
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(QuickStartProjectProvider));

    private readonly string _errorString;

    private readonly IQuickStartProjectProvider _quickStartProjectProvider;

    public string PackageFullName { get; }

    public string DisplayName { get; }

    public Uri TermsOfServiceUri { get; }

    public Uri PrivacyPolicyUri { get; }

    public string[] SamplePrompts { get; }

    public QuickStartProjectProvider(
        IQuickStartProjectProvider quickStartProjectProvider,
        ISetupFlowStringResource setupFlowStringResource,
        string packageFullName)
    {
        _quickStartProjectProvider = quickStartProjectProvider;
        PackageFullName = packageFullName;
        DisplayName = quickStartProjectProvider.DisplayName;
        TermsOfServiceUri = quickStartProjectProvider.TermsOfServiceUri;
        PrivacyPolicyUri = quickStartProjectProvider.PrivacyPolicyUri;
        _errorString = setupFlowStringResource.GetLocalized("QuickStartProjectUnexpectedError", DisplayName);
        SamplePrompts = quickStartProjectProvider.SamplePrompts;
    }

    public QuickStartProjectAdaptiveCardResult CreateAdaptiveCardSessionForExtensionInitialization()
    {
        try
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundExtensionInitialization");
            return _quickStartProjectProvider.CreateAdaptiveCardSessionForExtensionInitialization();
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"CreateAdaptiveCardSessionForExtensionInitialization for: {this} failed due to exception");
            TelemetryFactory.Get<ITelemetry>().LogException("CreateAdaptiveCardSessionForExtensionInitialization", ex);
            return new QuickStartProjectAdaptiveCardResult(ex, ex.Message, ex.Message);
        }
    }

    public IQuickStartProjectGenerationOperation CreateProjectGenerationOperation(string prompt, StorageFolder outputFolder)
    {
        try
        {
            TelemetryFactory.Get<ITelemetry>().LogCritical("QuickstartPlaygroundProjectGenerationOperation");
            return _quickStartProjectProvider.CreateProjectGenerationOperation(prompt, outputFolder);
        }
        catch (Exception ex)
        {
            TelemetryFactory.Get<ITelemetry>().LogException("QuickstartPlaygroundProjectGenerationOperation", ex);
            _log.Error(ex, $"CreateProjectGenerationOperation for: {this} failed due to exception");
            return null;
        }
    }

    public override string ToString()
    {
        return $"QuickStartProject provider: {DisplayName}";
    }
}
