// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.ViewModels;
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

    private readonly ILogger _logger;
    private readonly IWindowsPackageManager _wpm;
    private readonly IStringResource _stringResource;
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

    public SearchViewModel(ILogger logger, IWindowsPackageManager wpm, SetupFlowStringResource stringResource)
    {
        _wpm = wpm;
        _logger = logger;
        _stringResource = stringResource;
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
            var matches = await Task.Run(async () => await _wpm.AllCatalogs.SearchAsync(text, SearchResultLimit), cancellationToken);

            // Don't update the UI if the operation was canceled
            if (cancellationToken.IsCancellationRequested)
            {
                return (SearchResultStatus.Canceled, null);
            }

            // Update the UI only if the operation was successful
            SearchText = text;
            ResultPackages = matches.Select(m => new PackageViewModel(m)).ToList();
            return (SearchResultStatus.Ok, ResultPackages);
        }
        catch (OperationCanceledException)
        {
            return (SearchResultStatus.Canceled, null);
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(SearchViewModel), LogLevel.Info, $"Search error: {e.Message}");
            return (SearchResultStatus.ExceptionThrown, null);
        }
    }
}
