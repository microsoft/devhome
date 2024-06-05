// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Web;
using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Settings.ViewModels;
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
    private readonly IWinGet _winget;
    private readonly PackageProvider _packageProvider;
    private readonly SetupFlowOrchestrator _setupFlowOrchestrator;
    private static readonly char[] Separator = [','];

    public AppInstallActivationHandler(
        INavigationService navigationService,
        SetupFlowViewModel setupFlowViewModel,
        PackageProvider packageProvider,
        IWinGet winget,
        SetupFlowOrchestrator setupFlowOrchestrator)
    {
        _navigationService = navigationService;
        _setupFlowViewModel = setupFlowViewModel;
        _packageProvider = packageProvider;
        _winget = winget;
        _setupFlowOrchestrator = setupFlowOrchestrator;
    }

    protected override bool CanHandleInternal(ProtocolActivatedEventArgs args)
    {
        return args.Uri != null && args.Uri.AbsolutePath.Equals(AppSearchUri, StringComparison.OrdinalIgnoreCase);
    }

    protected async override Task HandleInternalAsync(ProtocolActivatedEventArgs args)
    {
        await AppActivationFlowAsync(args.Uri.Query);
    }

    private async Task AppActivationFlowAsync(string query)
    {
        try
        {
            // Don't interrupt the user if the machine configuration is in progress
            if (_setupFlowOrchestrator.IsMachineConfigurationInProgress)
            {
                _log.Warning("Cannot activate the add-apps-to-cart flow because the machine configuration is in progress");
                return;
            }
            else
            {
                _log.Information("Starting add-apps-to-cart activation");
                _navigationService.NavigateTo(typeof(SetupFlowViewModel).FullName!);
                _setupFlowViewModel.StartAppManagementFlow(query);
                await SearchAndSelectAsync(query);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error executing the add-apps-to-cart activation flow");
        }
    }

    private async Task SearchAndSelectAsync(string query)
    {
        var parameters = HttpUtility.ParseQueryString(query);
        var searchParameter = parameters["search"];

        if (string.IsNullOrEmpty(searchParameter))
        {
            _log.Warning("Search parameter is missing or empty in the query.");
            return;
        }

        // Currently using the first search term only
        var firstSearchTerm = searchParameter.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(term => term.Trim(' ', '"'))
                                             .FirstOrDefault();

        if (string.IsNullOrEmpty(firstSearchTerm))
        {
            _log.Warning("No valid search term was extracted from the query.");
            return;
        }

        var searchResults = await _winget.SearchAsync(firstSearchTerm, 1);
        if (searchResults.Count == 0)
        {
            _log.Warning("No results found for the search term: {SearchTerm}", firstSearchTerm);
            return;
        }

        var firstResult = _packageProvider.CreateOrGet(searchResults[0]);
        firstResult.IsSelected = true;
    }
}
