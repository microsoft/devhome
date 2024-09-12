// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Services;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.Models;
using DevHome.Customization.ViewModels;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Factories;
using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinRT;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly List<IExtensionWrapper> _repositorySourceControlProviders;

    private readonly INavigationService _navigationService;

    private readonly RepositoryManagementItemViewModelFactory _factory;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    private readonly Window _window;

    private readonly IExtensionService _extensionService;

    private readonly FileExplorerViewModel _sourceControlRegistrar;

    private readonly List<RepositoryManagementItemViewModel> _items = [];

    public ObservableCollection<RepositoryManagementItemViewModel> Items { get; private set; }

    [RelayCommand]
    public async Task AddExistingRepository()
    {
        try
        {
            _log.Information("Opening folder picker to select a new location");
            var existingRepositoryLocation = await _sourceControlRegistrar.AddFolderClick();

            if (string.IsNullOrEmpty(existingRepositoryLocation))
            {
                _log.Information("More than one repository was added");
                return;
            }

            _log.Information($"Selected '{existingRepositoryLocation}' for the repository path.");
            var foundSourceControlProvider = false;
            var extensionCLSID = string.Empty;
            IExtensionWrapper selectedExtension = null;
            foreach (var extension in _repositorySourceControlProviders)
            {
                extensionCLSID = extension?.ExtensionClassId ?? string.Empty;
                var didAdd = await _sourceControlRegistrar.AssignSourceControlProviderToRepository(extension, existingRepositoryLocation);

                if (didAdd)
                {
                    foundSourceControlProvider = true;
                    selectedExtension = extension;
                    break;
                }
            }

            if (foundSourceControlProvider)
            {
                string[] properties =
                [
                    "System.VersionControl.LastChangeAuthorName",
                    "System.VersionControl.LastChangeDate",
                    "System.VersionControl.LastChangeID",
                ];

                var blah = new SourceControlProvider();
                var thisProvider = blah.GetProvider(existingRepositoryLocation);
                var theseProperties = thisProvider.GetProperties(properties, existingRepositoryLocation);

                /*
                var theThing = selectedExtension.GetProviderAsync<ILocalRepositoryProvider>().Result;
                var theRepo = theThing.GetRepository(existingRepositoryLocation);
                var myProperties = theRepo.Repository.GetProperties(properties, existingRepositoryLocation);
                */

                /*
                MarshalInterface<ILocalRepositoryProvider>.FromAbi(providerPtr);
                var theThing = selectedExtension.GetProviderAsync<ILocalRepositoryProvider>().Result;
                var theRepo = theThing.GetRepository(existingRepositoryLocation.Path);
                var myProperties = theRepo.Repository.GetProperties(properties, existingRepositoryLocation.Path);
                */
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to open folder picker");
        }
    }

    [RelayCommand]
    public void NavigateToCloneRepositoryExpirence()
    {
        _navigationService.NavigateTo(KnownPageKeys.SetupFlow, KnownPageKeys.RepositoryConfiguration);
    }

    [RelayCommand]
    public void LoadRepositories()
    {
        _items.Clear();
        Items.Clear();
        var repositoriesFromTheDatabase = _dataAccessService.GetRepositories();
        ConvertToLineItems(repositoriesFromTheDatabase).ForEach(x => _items.Add(x));
        _items.Where(x => x.IsHiddenFromPage == false).ToList().ForEach(x => Items.Add(x));
    }

    [RelayCommand]
    public void HideRepository(RepositoryManagementItemViewModel repository)
    {
        if (repository == null)
        {
            return;
        }

        repository.RemoveThisRepositoryFromTheList();
        LoadRepositories();
    }

    public RepositoryManagementMainPageViewModel(
        RepositoryManagementItemViewModelFactory factory,
        RepositoryManagementDataAccessService dataAccessService,
        INavigationService navigationService,
        Window window,
        IExtensionService extensionService,
        FileExplorerViewModel sourceControlRegistrar)
    {
        _dataAccessService = dataAccessService;
        _factory = factory;
        Items = [];
        _navigationService = navigationService;
        _window = window;
        _extensionService = extensionService;
        _repositorySourceControlProviders = extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result.ToList();
        _sourceControlRegistrar = sourceControlRegistrar;
    }

    private List<RepositoryManagementItemViewModel> ConvertToLineItems(List<Repository> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repo in repositories)
        {
            // TODO: get correct values for branch and latest commit information
            var lineItem = _factory.MakeViewModel(repo.RepositoryName, repo.RepositoryClonePath, repo.IsHidden);
            lineItem.Branch = "Test Value";
            lineItem.LatestCommit = "Test Value";
            lineItem.HasAConfigurationFile = repo.HasAConfigurationFile;
            items.Add(lineItem);
        }

        return items;
    }
}
