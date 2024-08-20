// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;
using Windows.Foundation;

namespace SampleExtension.Providers;

public class NavigationProvider() : INavigationProvider
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(NavigationProvider)));
    private bool _disposedValue;

    private static ILogger Log => _logger.Value;

    string INavigationProvider.DisplayName => Resources.GetResource(@"NavigationProviderDisplayName", Log);

    public IAsyncOperation<NavigationPagesResult> GetNavigationPagesAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var pages = new List<NavigationPage>();
                var page = new NavigationPage();
                pages.Add(page);
                return new NavigationPagesResult(pages);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
                return new NavigationPagesResult(e, e.Message);
            }
        }).AsAsyncOperation();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
