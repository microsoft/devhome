// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;
public partial class AddViaAccountViewModel : ObservableObject
{
    public RepositoryProviders Providers { get; private set; }

    [ObservableProperty]
    private bool _isFetchingRepos;

    [ObservableProperty]
    private IEnumerable<DeveloperIdViewModel> _accounts;

    [ObservableProperty]
    private DeveloperIdViewModel _selectedAccount;

    [ObservableProperty]
    private ObservableCollection<RepositoryProviderViewModel> _providersToDisplay;

    [ObservableProperty]
    private RepositoryProviderViewModel _selectedProvider;

    [ObservableProperty]
    private IEnumerable<RepoViewListItem> _repositories;

    [ObservableProperty]
    private ObservableCollection<RepoViewListItem> _selectedRepositories;

    [ObservableProperty]
    private bool _canSelectAccounts;

    public bool CanSkipAccountPage
    {
        get;
    }

    [RelayCommand]
    public async void ChangeAccounts()
    {
        // This gets fired when events are removed from the account combo box.
        // When the provider combo box is changed all accounts are removed from the account combo box
        // and new accounts are added. This method fires twice.
        // Once to remove all accounts and once to add all logged in accounts.
        // GetRepositories sets the repositories list view.
        var repositories = await GetRepositoriesAsync();
        Repositories = OrderRepos(repositories);
        /*
        SelectRepositories(SetRepositories(SelectedProvider.DisplayName, accountName));
        */
    }

    public AddViaAccountViewModel()
    {
        Accounts = new List<DeveloperIdViewModel>();
        GetPlugins();

        var allProviders = Providers.GetAllProviders();
        if (allProviders.Count() == 1)
        {
            Providers.StartIfNotRunning(allProviders.First().ProviderName);
            var accounts = Providers.GetAllLoggedInAccounts(allProviders.First().ProviderName);
            if (accounts.Count() == 1)
            {
                CanSkipAccountPage = true;
                SelectedProvider = allProviders.First();
                SelectedAccount = new DeveloperIdViewModel(accounts.First());
            }
        }
    }

    /// <summary>
    /// Gets all the repositories for the specified provider and account.
    /// </summary>
    /// <remarks>
    /// The side effect of this method is _repositoriesForAccount is populated with repositories.
    /// </remarks>
    /// <param name="repositoryProvider">The provider.  This should match the display name of the plugin</param>
    /// <param name="loginId">The login Id to get the repositories for</param>
    public async Task<IEnumerable<IRepository>> GetRepositoriesAsync()
    {
        IsFetchingRepos = true;
        IEnumerable<IRepository> repositories = new List<IRepository>();
        await Task.Run(() =>
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Measure, new RepoToolEvent("GettingAllLoggedInAccounts"));
            var loggedInDeveloper = Providers.GetAllLoggedInAccounts(SelectedProvider.ProviderName).FirstOrDefault(x => x.LoginId() == SelectedAccount.LoginId);

            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetRepos_Event", LogLevel.Measure, new RepoToolEvent("GettingAllRepos"));
            repositories = Providers.GetAllRepositories(SelectedProvider.ProviderName, loggedInDeveloper);
        });

        IsFetchingRepos = false;
        return repositories;
    }

    /// <summary>
    /// Order repos in a particular order.  The order is
    /// 1. User Private repos
    /// 2. Org repos
    /// 3. User Public repos.
    /// Each section is ordered by the most recently updated.
    /// </summary>
    /// <param name="repos">The list of repos to order.</param>
    /// <returns>An enumerable collection of items ready to be put into the ListView</returns>
    private IEnumerable<RepoViewListItem> OrderRepos(IEnumerable<IRepository> repos)
    {
        var organizationRepos = repos.Where(x => !x.OwningAccountName.Equals(SelectedAccount.LoginId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        var userRepos = repos.Where(x => x.OwningAccountName.Equals(SelectedAccount.LoginId, StringComparison.OrdinalIgnoreCase));
        var userPublicRepos = userRepos.Where(x => !x.IsPrivate)
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        var userPrivateRepos = userRepos.Where(x => x.IsPrivate)
            .OrderByDescending(x => x.LastUpdated)
            .Select(x => new RepoViewListItem(x));

        return userPrivateRepos
            .Concat(organizationRepos)
            .Concat(userPublicRepos);
    }

    /// <summary>
    /// Updates the UI with the repositories to display for the specific user and provider.
    /// </summary>
    /// <param name="repositoryProvider">The name of the provider</param>
    /// <param name="loginId">The login ID</param>
    /// <returns>All previously selected repos excluding any added via URL.</returns>
    public IEnumerable<RepoViewListItem> SetRepositories(string repositoryProvider, string loginId)
    {
        /*
        Repositories = OrderRepos(Repositories);

        return _previouslySelectedRepos.Where(x => x.OwningAccount != null)
            .Where(x => x.PluginName.Equals(repositoryProvider, StringComparison.OrdinalIgnoreCase)
            && x.OwningAccount.LoginId().Equals(loginId, StringComparison.OrdinalIgnoreCase))
            .Select(x => new RepoViewListItem(x.RepositoryToClone));
        */

        return new List<RepoViewListItem>();
    }

    [RelayCommand]
    public void FilterRepos(string searchString)
    {
        /*
        // Just in case something other than a text box calls this.
        if (sender is TextBox)
        {
            AddRepoViewModel.FilterRepositories(FilterTextBox.Text);
            SelectRepositories(AddRepoViewModel.EverythingToClone);
        }
        */
    }

    [RelayCommand]
    public void AddOrRemoveRepos()
    {
        /*
        var loginId = (string)AccountsComboBox.SelectedValue;
        var providerName = (string)RepositoryProviderComboBox.SelectedValue;

        AddRepoViewModel.AddOrRemoveRepository(providerName, loginId, e.AddedItems, e.RemovedItems);
        ToggleCloneButton();
        */
    }

    /// <summary>
    /// Gets all the plugins the DevHome can see.
    /// </summary>
    /// <remarks>
    /// A valid plugin is one that has a repository provider and developerId provider.
    /// </remarks>
    public void GetPlugins()
    {
        Log.Logger?.ReportInfo(Log.Component.RepoConfig, "Getting installed plugins with Repository and DevId providers");
        var pluginService = Application.Current.GetService<IPluginService>();
        var pluginWrappers = pluginService.GetInstalledPluginsAsync().Result;
        var plugins = pluginWrappers.Where(
            plugin => plugin.HasProviderType(ProviderType.Repository) &&
            plugin.HasProviderType(ProviderType.DeveloperId));

        Providers = new RepositoryProviders(plugins);

        // Start all plugins to get the DisplayName of each provider.
        Providers.StartAllPlugins();

        ProvidersToDisplay = new ObservableCollection<RepositoryProviderViewModel>(Providers.GetAllProviders());
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_SearchForProviders_Event", LogLevel.Measure, new ProviderEvent(ProvidersToDisplay.Count));
    }

    public bool ValidateRepos()
    {
        return SelectedRepositories.Count > 0;
    }

    /// <summary>
    /// Gets all the accounts for a provider and updates the UI.
    /// </summary>
    /// <param name="repositoryProviderName">The provider the user wants to use.</param>
    public async Task GetAccountsAsync(string repositoryProviderName)
    {
        await Task.Run(() => Providers.StartIfNotRunning(repositoryProviderName));
        Accounts = await Task.Run(() => Providers.GetAllLoggedInAccounts(repositoryProviderName)
        .Select(x => new DeveloperIdViewModel(x)));
        if (!Accounts.Any())
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Measure, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: false));

            // Throw away developerId because DevHome allows one account per provider. GetAllLoggedInAccounts is called
            // in anticipation of 1 Provider : N DeveloperIds
            await Task.Run(() => Providers.LogInToProvider(repositoryProviderName));
            Accounts = await Task.Run(() => Providers.GetAllLoggedInAccounts(repositoryProviderName)
            .Select(x => new DeveloperIdViewModel(x)));
        }
        else
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Measure, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: true));
        }
    }
}
