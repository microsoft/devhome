// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using static DevHome.SetupFlow.RepoConfig.Models.Common;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;

/// <summary>
/// View model to handle the top layer of the repo tool including
/// 1. Repo Review
/// 2. Switching between account, repositories, and url page
/// </summary>
public partial class AddRepoViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the list that keeps all repositories the user wants to clone.
    /// </summary>
    public List<CloningInformation> EverythingToClone
    {
        get; set;
    }

    /// <summary>
    /// The url of the repository the user wants to clone.
    /// </summary>
    [ObservableProperty]
    private string _url = string.Empty;

    /// <summary>
    /// All the providers Dev Home found.  Used for logging in the accounts and getting all repositories.
    /// </summary>
    private RepositoryProviders _providers;

    /// <summary>
    /// The list of all repositories shown to the user on the repositories page.
    /// </summary>
    private IEnumerable<IRepository> _repositoriesForAccount;

    /// <summary>
    /// Names of all providers.  This is shown to the user on the accounts page.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _providerNames = new ();

    /// <summary>
    /// Names of all accounts the user has logged into for a particular provider.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _accounts = new ();

    /// <summary>
    /// All the repositories for a specific account.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<string> _repositories = new ();

    /// <summary>
    /// Should the URL page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showUrlPage;

    /// <summary>
    /// Should the account page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showAccountPage;

    /// <summary>
    /// Should the repositories page be visible?
    /// </summary>
    [ObservableProperty]
    private Visibility _showRepoPage;

    /// <summary>
    /// Should the error text be shown?
    /// </summary>
    [ObservableProperty]
    private Visibility _showErrorTextBox;

    /// <summary>
    /// Keeps track of if the account button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isAccountToggleButtonChecked;

    /// <summary>
    /// Keeps track if the URL button is checked.  Used to switch UIs
    /// </summary>
    [ObservableProperty]
    private bool? _isUrlAccountButtonChecked;

    /// <summary>
    /// COntrols if the primary button is enabled.  Turns true if everything is correct.
    /// </summary>
    [ObservableProperty]
    private bool _shouldPrimaryButtonBeEnabled;

    [ObservableProperty]
    private string _textToFilterBy;

    [RelayCommand]
    private void FilterRepositories(string text)
    {
        IEnumerable<string> filteredRepositories;
        if (text.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            filteredRepositories = _repositoriesForAccount.OrderBy(x => x.IsPrivate).Select(x => x.DisplayName);
        }
        else
        {
            filteredRepositories = _repositoriesForAccount.OrderBy(x => x.IsPrivate).Where(x => x.DisplayName.StartsWith(text, StringComparison.OrdinalIgnoreCase)).Select(x => x.DisplayName);
        }

        Repositories = new ObservableCollection<string>(filteredRepositories);
    }

    /// <summary>
    /// Gets or sets what page the user is currently on.  Used to branch logic depending on the page.
    /// </summary>
    internal PageKind CurrentPage
    {
        get; set;
    }

    public AddRepoViewModel()
    {
        ChangeToUrlPage();

        // override changes ChangeToUrlPage to correctly set the state.
        IsUrlAccountButtonChecked = true;
        IsAccountToggleButtonChecked = false;
        ShouldPrimaryButtonBeEnabled = false;
        ShowErrorTextBox = Visibility.Collapsed;
        EverythingToClone = new ();
    }

    /// <summary>
    /// Gets all the plugins the DevHome can see.
    /// </summary>
    /// <remarks>
    /// A valid plugin is one that has a repository provider and devid provider.
    /// </remarks>
    public void GetPlugins()
    {
        var pluginService = Application.Current.GetService<IPluginService>();
        var pluginWrappers = pluginService.GetInstalledPluginsAsync().Result;
        var plugins = pluginWrappers.Where(
            plugin => plugin.HasProviderType(ProviderType.Repository) &&
            plugin.HasProviderType(ProviderType.DevId));

        _providers = new RepositoryProviders(plugins);

        ProviderNames = new ObservableCollection<string>(_providers.GetAllProviderNames());
    }

    public void ChangeToUrlPage()
    {
        ShowUrlPage = Visibility.Visible;
        ShowAccountPage = Visibility.Collapsed;
        ShowRepoPage = Visibility.Collapsed;
        IsUrlAccountButtonChecked = true;
        IsAccountToggleButtonChecked = false;
        CurrentPage = PageKind.AddViaUrl;
    }

    public void ChangeToAccountPage()
    {
        ShowUrlPage = Visibility.Collapsed;
        ShowAccountPage = Visibility.Visible;
        ShowRepoPage = Visibility.Collapsed;
        IsUrlAccountButtonChecked = false;
        IsAccountToggleButtonChecked = true;
        CurrentPage = PageKind.AddViaAccount;
    }

    public void ChangeToRepoPage()
    {
        ShowUrlPage = Visibility.Collapsed;
        ShowAccountPage = Visibility.Collapsed;
        ShowRepoPage = Visibility.Visible;
        CurrentPage = PageKind.Repositories;

        // The only way to get the repo page is through the account page.
        // No need to change toggle buttons.
    }

    /// <summary>
    /// Makes sure all needed information is present.
    /// </summary>
    /// <returns>True if all information is in order, otherwise false</returns>
    public bool ValidateRepoInformation()
    {
        if (CurrentPage == PageKind.AddViaUrl)
        {
            // check if url or username/repo is filled in.
            return !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrEmpty(Url);
        }
        else if (CurrentPage == PageKind.AddViaAccount || CurrentPage == PageKind.Repositories)
        {
            // make sure the user has selected some repositories.
            return EverythingToClone.Count > 0;
        }
        else
        {
            return false;
        }
    }

    public void EnablePrimaryButton()
    {
        ShouldPrimaryButtonBeEnabled = true;
    }

    public void DisablePrimaryButton()
    {
        ShouldPrimaryButtonBeEnabled = false;
    }

    /// <summary>
    /// Gets all the accounts for a provider and updates the UI.
    /// </summary>
    /// <param name="repositoryProviderName">The provider the user wants to use.</param>
    public void GetAccounts(string repositoryProviderName)
    {
        _providers.StartIfNotRunning(repositoryProviderName);
        var loggedInAccounts = _providers.GetAllLoggedInAccounts(repositoryProviderName);
        if (!loggedInAccounts.Any())
        {
            // Throw away developer id becase we're calling GetAllLoggedInAccounts in anticipation
            // of 1 Provider : N DeveloperIds
            _providers.LogInToProvider(repositoryProviderName);
            loggedInAccounts = _providers.GetAllLoggedInAccounts(repositoryProviderName);
        }

        Accounts = new ObservableCollection<string>(loggedInAccounts.Select(x => x.LoginId()));
    }

    /// <summary>
    /// Adds repositories to the list of repos to clone.
    /// Removes repositories from the list of repos to clone.
    /// </summary>
    /// <param name="providerName">The provider that is used to do the cloning.</param>
    /// <param name="accountName">The account used to authenticate into the provider.</param>
    /// <param name="repositoriesToAdd">Repositories to add</param>
    /// <param name="repositoriesToRemove">Repositories to remove.</param>
    public void AddOrRemoveRepository(string providerName, string accountName, IList<object> repositoriesToAdd, IList<object> repositoriesToRemove)
    {
        var developerId = _providers.GetAllLoggedInAccounts(providerName).FirstOrDefault(x => x.LoginId() == accountName);
        foreach (string repositoryToRemove in repositoriesToRemove)
        {
            var cloningInformation = new CloningInformation();
            cloningInformation.ProviderName = providerName;
            cloningInformation.OwningAccount = developerId;
            cloningInformation.RepositoryToClone = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName == repositoryToRemove);

            EverythingToClone.Remove(cloningInformation);
        }

        foreach (string repositoryToAdd in repositoriesToAdd)
        {
            var cloningInformation = new CloningInformation();
            cloningInformation.ProviderName = providerName;
            cloningInformation.OwningAccount = developerId;
            cloningInformation.RepositoryToClone = _repositoriesForAccount.FirstOrDefault(x => x.DisplayName == repositoryToAdd);

            EverythingToClone.Add(cloningInformation);
        }
    }

    /// <summary>
    /// Adds a repository from the URL page.
    /// </summary>
    /// <param name="cloneLocation">The location to clone the repo to</param>
    public void AddRepositoryViaUri(string cloneLocation)
    {
        // Try to parse repo from Uri
        // null means no providers were able to parse the Uri.
        var providerNameAndRepo = _providers.ParseRepositoryFromUri(new Uri(Url));
        if (providerNameAndRepo.Item2 == null)
        {
            return;
        }

        var repository = providerNameAndRepo.Item2;
        var developerId = new DeveloperId(repository.OwningAccountName, string.Empty, repository.OwningAccountName, Url);
        var cloningInformation = new CloningInformation();
        cloningInformation.ProviderName = providerNameAndRepo.Item1;
        cloningInformation.OwningAccount = developerId;
        cloningInformation.RepositoryToClone = repository;
        cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);

        EverythingToClone.Add(cloningInformation);
    }

    /// <summary>
    /// Gets all the repositories for the the specified provider and account.
    /// </summary>
    /// <param name="repositoryProvider">The provider.  This should match IRepositoryProvider.LoginId</param>
    /// <param name="loginId">The login Id to get the repositories for</param>
    public void GetRepositories(string repositoryProvider, string loginId)
    {
        var loggedInDeveloper = _providers.GetAllLoggedInAccounts(repositoryProvider).FirstOrDefault(x => x.LoginId() == loginId);
        _repositoriesForAccount = _providers.GetAllRepositories(repositoryProvider, loggedInDeveloper);

        // TODO: What if the user comes back here with repos selected?
        Repositories = new ObservableCollection<string>(_repositoriesForAccount.OrderBy(x => x.IsPrivate).Select(x => x.DisplayName));
    }

    /// <summary>
    /// Sets the clone location for all repositories to cloneLocation
    /// </summary>
    /// <param name="cloneLocation">The location to clone all repositories to.</param>
    public void SetCloneLocation(string cloneLocation)
    {
        foreach (var cloningInformation in EverythingToClone)
        {
            cloningInformation.CloningLocation = new DirectoryInfo(cloneLocation);
        }
    }
}
