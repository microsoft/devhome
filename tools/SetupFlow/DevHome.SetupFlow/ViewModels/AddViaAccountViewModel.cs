// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.TelemetryEvents.SetupFlow;
using DevHome.SetupFlow.Models;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;
public partial class AddViaAccountViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isFetchingRepos;

    [ObservableProperty]
    private IEnumerable<IDeveloperId> _accounts;

    [ObservableProperty]
    private IDeveloperId _selectedAccount;

    [ObservableProperty]
    private IEnumerable<RepositoryProvider> _providers;

    [ObservableProperty]
    private RepositoryProvider _selectedProvider;

    [ObservableProperty]
    private IEnumerable<IRepository> _repositories;

    [ObservableProperty]
    private ObservableCollection<IRepository> _selectedRepositories;

    [ObservableProperty]
    private bool _canSelectAccounts;

    private RepositoryProviders _repositoryProviders = new (null);

    public bool CanSkipAccountPage { get; }

    [RelayCommand]
    public void ChangeRepositoryProvider(string providerName)
    {
        /*
         *          var repositoryProviderName = (string)RepositoryProviderComboBox.SelectedItem;
            if (!string.IsNullOrEmpty(repositoryProviderName))
            {
                PrimaryButtonStyle = AddRepoStackPanel.Resources["ContentDialogLogInButtonStyle"] as Style;
                IsPrimaryButtonEnabled = true;
            }
            else
            {
                PrimaryButtonStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;
                IsPrimaryButtonEnabled = false;
            }
         */
    }

    [RelayCommand]
    public void ChangeAccounts(string accountName)
    {
        /*
        // This gets fired when events are removed from the account combo box.
        // When the provider combo box is changed all accounts are removed from the account combo box
        // and new accounts are added. This method fires twice.
        // Once to remove all accounts and once to add all logged in accounts.
        // GetRepositories sets the repositories list view.
        if (e.AddedItems.Count > 0)
        {
            var loginId = (string)AccountsComboBox.SelectedValue;
            var providerName = (string)RepositoryProviderComboBox.SelectedValue;
            await AddRepoViewModel.GetRepositoriesAsync(providerName, loginId);
            SelectRepositories(AddRepoViewModel.SetRepositories(providerName, loginId));
        }
        */
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
    /// Gets all the accounts for a provider and updates the UI.
    /// </summary>
    /// <param name="repositoryProviderName">The provider the user wants to use.</param>
    public async Task GetAccountsAsync(string repositoryProviderName)
    {
        await Task.Run(() => _repositoryProviders.StartIfNotRunning(repositoryProviderName));
        Accounts = await Task.Run(() => _repositoryProviders.GetAllLoggedInAccounts(repositoryProviderName));
        if (!Accounts.Any())
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Measure, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: false));

            // Throw away developerId because DevHome allows one account per provider. GetAllLoggedInAccounts is called
            // in anticipation of 1 Provider : N DeveloperIds
            await Task.Run(() => _repositoryProviders.LogInToProvider(repositoryProviderName));
            Accounts = await Task.Run(() => _repositoryProviders.GetAllLoggedInAccounts(repositoryProviderName));
        }
        else
        {
            TelemetryFactory.Get<ITelemetry>().Log("RepoTool_GetAccount_Event", LogLevel.Measure, new RepoDialogGetAccountEvent(repositoryProviderName, alreadyLoggedIn: true));
        }
    }
}
