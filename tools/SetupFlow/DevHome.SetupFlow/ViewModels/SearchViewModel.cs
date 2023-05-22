// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.TelemetryEvents;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.ViewModels;
public partial class SearchViewModel : ObservableObject
{
    public enum SearchResultStatus
    {
        // Search was successful
        Ok,

        // Search was performed on a null, empty or whitespace string
        EmptySearchQuery,

        // Search canceled
        Canceled,

        // Search aborted because catalog is not connected yet
        CatalogNotConnect,

        // Exception thrown during search
        ExceptionThrown,
    }

    private readonly IWindowsPackageManager _wpm;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly PackageProvider _packageProvider;
    private const int SearchResultLimit = 20;

    /// <summary>
    /// Search query text
    /// </summary>
    [ObservableProperty]
    private string _searchText;

    /// <summary>
    /// List of search results
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SearchCountText))]
    [NotifyPropertyChangedFor(nameof(NoSearchResultsText))]
    private List<PackageViewModel> _resultPackages = new ();

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.ResultCount"/>
    /// </summary>
    public string SearchCountText => _stringResource.GetLocalized(StringResourceKey.ResultCount, ResultPackages.Count);

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.NoSearchResultsFoundTitle"/>
    /// </summary>
    public string NoSearchResultsText => _stringResource.GetLocalized(StringResourceKey.NoSearchResultsFoundTitle, SearchText);

    public SearchViewModel(IWindowsPackageManager wpm, ISetupFlowStringResource stringResource, PackageProvider packageProvider)
    {
        _wpm = wpm;
        _stringResource = stringResource;
        _packageProvider = packageProvider;
    }

    /// <summary>
    /// Search for packages in all remote and local catalogs
    /// </summary>
    /// <param name="text">Text search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search status and result</returns>
    public async Task<(SearchResultStatus, List<PackageViewModel>)> SearchAsync(string text, CancellationToken cancellationToken)
    {
        // Skip search if text is empty
        if (string.IsNullOrWhiteSpace(text))
        {
            return (SearchResultStatus.EmptySearchQuery, null);
        }

        // Connect is required before searching
        if (!_wpm.AllCatalogs.IsConnected)
        {
            return (SearchResultStatus.CatalogNotConnect, null);
        }

        try
        {
            // Run the search on a separate (non-UI) thread to prevent lagging the UI.
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Running package search for query [{text}]");
            TelemetryFactory.Get<ITelemetry>().LogMeasure("Search_SerchingForApplication_Event");
            var matches = await Task.Run(async () => await _wpm.AllCatalogs.SearchAsync(text, SearchResultLimit), cancellationToken);

            // Don't update the UI if the operation was canceled
            if (cancellationToken.IsCancellationRequested)
            {
                return (SearchResultStatus.Canceled, null);
            }

            // Update the UI only if the operation was successful
            SearchText = text;
            ResultPackages = await Task.Run(() => matches.Select(m => _packageProvider.CreateOrGet(m)).ToList());
            return (SearchResultStatus.Ok, ResultPackages);
        }
        catch (OperationCanceledException)
        {
            return (SearchResultStatus.Canceled, null);
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Search error.", e);
            return (SearchResultStatus.ExceptionThrown, null);
        }
    }
}
