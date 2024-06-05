// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Exceptions;
using DevHome.SetupFlow.Exceptions;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Serilog;

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

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SearchViewModel));
    private readonly IWinGet _wpm;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly PackageProvider _packageProvider;
    private readonly IScreenReaderService _screenReaderService;
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
    private List<PackageViewModel> _resultPackages = new();

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.ResultCount"/>
    /// </summary>
    public string SearchCountText => ResultPackages.Count == 1 ? _stringResource.GetLocalized(StringResourceKey.ResultCountSingular, ResultPackages.Count, SearchText) : _stringResource.GetLocalized(StringResourceKey.ResultCountPlural, ResultPackages.Count, SearchText);

    /// <summary>
    /// Gets the localized string for <see cref="StringResourceKey.NoSearchResultsFoundTitle"/>
    /// </summary>
    public string NoSearchResultsText => _stringResource.GetLocalized(StringResourceKey.NoSearchResultsFoundTitle, SearchText);

    public SearchViewModel(IWinGet wpm, ISetupFlowStringResource stringResource, PackageProvider packageProvider, IScreenReaderService screenReaderService)
    {
        _wpm = wpm;
        _stringResource = stringResource;
        _packageProvider = packageProvider;
        _screenReaderService = screenReaderService;
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

        try
        {
            // Run the search on a separate (non-UI) thread to prevent lagging the UI.
            _log.Information($"Running package search for query [{text}]");
            var matches = await Task.Run(async () => await _wpm.SearchAsync(text, SearchResultLimit), cancellationToken);

            // Don't update the UI if the operation was canceled
            if (cancellationToken.IsCancellationRequested)
            {
                return (SearchResultStatus.Canceled, null);
            }

            // Update the UI only if the operation was successful
            SearchText = text;
            ResultPackages = await Task.Run(() => matches.Select(m => _packageProvider.CreateOrGet(m)).ToList());

            // Announce the results.
            if (ResultPackages.Count != 0)
            {
                TelemetryFactory.Get<ITelemetry>().Log("Search_SearchingForApplication_Found_Event", LogLevel.Critical, new SearchEvent());
                _screenReaderService.Announce(SearchCountText);
            }
            else
            {
                TelemetryFactory.Get<ITelemetry>().Log("Search_SearchingForApplication_NotFound_Event", LogLevel.Critical, new SearchEvent());
                _screenReaderService.Announce(NoSearchResultsText);
            }

            return (SearchResultStatus.Ok, ResultPackages);
        }
        catch (WindowsPackageManagerRecoveryException)
        {
            return (SearchResultStatus.CatalogNotConnect, null);
        }
        catch (OperationCanceledException)
        {
            return (SearchResultStatus.Canceled, null);
        }
        catch (Exception e)
        {
            _log.Error(e, $"Search error.");
            return (SearchResultStatus.ExceptionThrown, null);
        }
    }
}
