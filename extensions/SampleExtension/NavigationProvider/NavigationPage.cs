// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;
using Windows.Storage.Streams;

namespace SampleExtension.Providers;

internal sealed class NavigationPage : INavigationPage
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(NavigationPage)));

    private static ILogger Log => _logger.Value;

    public string DisplayName => Resources.GetResource(@"NavigationProviderDisplayName", Log);

    public uint DisplayRank => 1u;

    public bool Enabled => true;

    public string MenuDisplayName => Resources.GetResource(@"NavigationProviderDisplayName", Log);

    public IRandomAccessStreamReference MenuIcon => throw new NotImplementedException();

    public NavigationPage()
    {
    }

    public AdaptiveCardSessionResult GetNavigationAdaptiveCardSession()
    {
        Log.Information($"GetNavigationAdaptiveCardSession");
        return new AdaptiveCardSessionResult(new NavigationUIController());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
