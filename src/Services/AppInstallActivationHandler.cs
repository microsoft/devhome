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
/// Class that handles the activation of the application when an add-apps-to-cart URI protocol is used.
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
    private readonly Window _mainWindow;
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
        Window mainWindow)
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
            foreach (ActivationQueryType queryType in Enum.GetValues(typeof(ActivationQueryType)))
            {
                var query = parameters.Get(queryType.ToString());

                if (!string.IsNullOrEmpty(query))
                {
                    await AppActivationFlowAsync(query, queryType);
                    return; // Exit after handling the first non-null query
                }
            }
        }
    }

    private async Task AppActivationFlowAsync(string query, ActivationQueryType queryType)
    {
        if (_setupFlowOrchestrator.IsMachineConfigurationInProgress)
        {
            _log.Warning($"Cannot activate the {AppSearchUri} flow because the machine configuration is in progress");
            await _mainWindow.ShowErrorMessageDialogAsync(
                    _setupFlowStringResource.GetLocalized(StringResourceKey.AppInstallActivationTitle),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.URIActivationFailedBusy),
                    _setupFlowStringResource.GetLocalized(StringResourceKey.Close));
            return;
        }

        var identifiers = SplitAndTrimIdentifiers(query);
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

    private string[] SplitAndTrimIdentifiers(string query)
    {
        return query.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim(' ', '"'))
                    .ToArray();
    }

    private async Task HandleAppSelectionAsync(string[] identifiers, ActivationQueryType queryType)
    {
        try
        {
            switch (queryType)
            {
                case ActivationQueryType.Search:
                    await SearchAndSelectAsync(identifiers[0]);
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
        List<WinGetPackageUri> uris = [];

        foreach (var identifier in identifiers)
        {
            uris.Add(new WinGetPackageUri(identifier));
        }

        try
        {
            var list = await _windowsPackageManager.GetPackagesAsync(uris);
            foreach (var item in list)
            {
                var package = _packageProvider.CreateOrGet(item);
                package.IsSelected = true;
                _log.Information($"Selected package: {item} for addition to cart.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error occurred during package search for URIs: {uris}.");
        }
    }

    private async Task SearchAndSelectAsync(string identifier)
    {
        var searchResults = await _windowsPackageManager.SearchAsync(identifier, 1);
        if (searchResults.Count == 0)
        {
            _log.Warning($"No results found for the identifier: {identifier}");
        }
        else
        {
            var package = _packageProvider.CreateOrGet(searchResults[0]);
            package.IsSelected = true;
            _log.Information($"Selected package: {package} for addition to cart.");
        }
    }
}
