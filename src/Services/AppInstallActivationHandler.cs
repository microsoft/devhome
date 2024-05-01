// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Web;
using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.ViewModels;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace DevHome.Services;

/// <summary>
/// Class that handles the activation of the application when an add-apps-to-cart URI protcol is used.
/// </summary>
public class AppInstallActivationHandler : ActivationHandler<ProtocolActivatedEventArgs>
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppInstallActivationHandler));
    private const string AppSearchUri = "add-apps-to-cart";
    private readonly INavigationService _navigationService;
    private readonly SetupFlowViewModel _setupFlowViewModel;
    private readonly IWindowsPackageManager _windowsPackageManager;
    private readonly PackageProvider _packageProvider;
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private readonly WindowEx _mainWindow;
    private readonly ISetupFlowStringResource _setupFlowStringResource;
    private static readonly char[] Separator = [','];

    public enum ActivationQueryType
    {
        Search,
        WingetURIs,
    }

    public AppInstallActivationHandler(
        INavigationService navigationService,
        SetupFlowViewModel setupFlowViewModel,
        PackageProvider packageProvider,
        IWindowsPackageManager wpm,
        SetupFlowOrchestrator setupFlowOrchestrator,
        ISetupFlowStringResource setupFlowStringResource,
        WindowEx mainWindow)
    {
        _navigationService = navigationService;
        _setupFlowViewModel = setupFlowViewModel;
        _packageProvider = packageProvider;
        _windowsPackageManager = wpm;
        _setupFlowOrchestrator = setupFlowOrchestrator;
        _setupFlowStringResource = setupFlowStringResource;
        _mainWindow = mainWindow;
    }

    protected override bool CanHandleInternal(ProtocolActivatedEventArgs args)
    {
        return args.Uri != null && args.Uri.AbsolutePath.Equals(AppSearchUri, StringComparison.OrdinalIgnoreCase);
    }

    protected async override Task HandleInternalAsync(ProtocolActivatedEventArgs args)
    {
        var uri = args.Uri;
        var parameters = HttpUtility.ParseQueryString(uri.Query);

        if (parameters != null)
        {
            // TODO should probably make these case insensitive
            var searchQuery = parameters.Get("search");
            var wingetURIs = parameters.Get("URIs");

            if (!string.IsNullOrEmpty(searchQuery))
            {
                await AppActivationFlowAsync(searchQuery, ActivationQueryType.Search);
            }
            else if (!string.IsNullOrEmpty(wingetURIs))
            {
                await AppActivationFlowAsync(wingetURIs, ActivationQueryType.WingetURIs);
            }
        }
    }

    private async Task AppActivationFlowAsync(string query, ActivationQueryType queryType)
    {
        if (_setupFlowOrchestrator.IsMachineConfigurationInProgress)
        {
            _log.Warning("Cannot activate the add-apps-to-cart flow because the machine configuration is in progress");
            await _mainWindow.ShowErrorMessageDialogAsync(
                    _setupFlowStringResource.GetLocalized(StringResourceKey.AppInstallActivationTitle),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.URIActivationFailedBusy),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.Close));
            return;
        }

        var identifiers = ParseIdentifiers(query, queryType);
        if (identifiers.Length == 0)
        {
            _log.Warning("No valid identifiers provided in the query.");
            return;
        }

        _log.Information($"Starting {AppSearchUri} activation");
        _navigationService.NavigateTo(typeof(SetupFlowViewModel).FullName!);
        _setupFlowViewModel.StartAppManagementFlow(queryType == ActivationQueryType.Search ? identifiers[0] : null);
        await HandleAppSelectionAsync(identifiers, queryType);
    }

    private string[] ParseIdentifiers(string query, ActivationQueryType queryType)
    {
        switch (queryType)
        {
            case ActivationQueryType.Search:
                var terms = query.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                if (terms.Length > 0)
                {
                    var firstTerm = terms[0].Replace("\"", string.Empty).Trim();
                    return string.IsNullOrEmpty(firstTerm) ? Array.Empty<string>() : new[] { firstTerm };
                }

                return Array.Empty<string>();

            case ActivationQueryType.WingetURIs:
                return query.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => id.Trim(' ', '"'))
                            .ToArray();

            default:
                _log.Warning("Unsupported activation query type: {QueryType}", queryType);
                return Array.Empty<string>();
        }
    }

    private async Task HandleAppSelectionAsync(string[] identifiers, ActivationQueryType queryType)
    {
        try
        {
            switch (queryType)
            {
                case ActivationQueryType.Search:
                    await SearchAndSelectAsync(identifiers);
                    return;

                case ActivationQueryType.WingetURIs:
                    await PackageSearchAsync(identifiers);
                    return;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error executing the {AppSearchUri} activation flow");
        }
    }

    private async Task PackageSearchAsync(string[] identifiers)
    {
        if (identifiers == null || identifiers.Length == 0)
        {
            _log.Warning("No valid identifiers provided in the query.");
            return;
        }

        List<WinGetPackageUri> uris = [];

        foreach (var identifier in identifiers)
        {
            // ensure we handle the case where the identifier is invalid.
            uris.Add(new WinGetPackageUri(identifier));
        }

        var list = await _windowsPackageManager.GetPackagesAsync(uris);
        foreach (var item in list)
        {
            var package = _packageProvider.CreateOrGet(item);
            package.IsSelected = true;
            _log.Information("Selected package with identifier {Identifier} for addition to cart.", item);
        }
    }

    private async Task SearchAndSelectAsync(string[] identifiers)
    {
        if (identifiers == null || identifiers.Length == 0)
        {
            _log.Warning("No valid identifiers provided in the query.");
            return;
        }

        foreach (var identifier in identifiers)
        {
            var searchResults = await _windowsPackageManager.SearchAsync(identifier, 1);
            if (searchResults.Count == 0)
            {
                _log.Warning("No results found for the identifier: {Identifier}", identifier);
                continue;
            }

            var package = _packageProvider.CreateOrGet(searchResults[0]);
            package.IsSelected = true;
            _log.Information("Selected package with identifier {Identifier} for addition to cart.", identifier);
        }
    }
}
