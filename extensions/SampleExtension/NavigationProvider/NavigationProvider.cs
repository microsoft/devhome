// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;

namespace SampleExtension.Providers;

public class NavigationProvider() : INavigationProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(NavigationProvider)));

    private static ILogger Log => _logger.Value;

    string INavigationProvider.DisplayName => Resources.GetResource(@"NavigationProviderDisplayName", Log);

    public Uri Icon => throw new NotImplementedException();

    public string PageDisplayName => throw new NotImplementedException();

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        Log.Information($"GetNavigationAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new NavigationUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public AdaptiveCardSessionResult GetNavigationAdaptiveCardSession() => throw new NotImplementedException();
}
