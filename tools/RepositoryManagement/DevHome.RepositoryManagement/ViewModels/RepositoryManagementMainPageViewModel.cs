// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using DevHome.SetupFlow.Common.Helpers;
using FileExplorerGitIntegration.Models;
using FileExplorerSourceControlIntegration;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinRT;
using YamlDotNet.Core;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private static readonly string[] _properties =
    [
        "System.VersionControl.LastChangeAuthorName",
        "System.VersionControl.LastChangeDate",
        "System.VersionControl.LastChangeID",
    ];

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
                if (didAdd.Result == Customization.Helpers.ResultType.Success)
                {
                    foundSourceControlProvider = true;
                    selectedExtension = extension;
                    break;
                }
            }

            if (foundSourceControlProvider)
            {
                // Get url and branch name
                var thisRepoOfSorts = new LibGit2Sharp.Repository(existingRepositoryLocation);
                var urlOfRepo = thisRepoOfSorts.Network.Remotes.First().Url;
                var localBranch = thisRepoOfSorts.Head.FriendlyName;

                // get most recent commit information
                var blah = new SourceControlProvider();
                var thisProvider = blah.GetProvider(existingRepositoryLocation);
                var helloAgain = thisProvider.GetProperties(_properties, string.Empty);
                var authorName = helloAgain["System.VersionControl.LastChangeAuthorName"];
                var lastChangedDate = helloAgain["System.VersionControl.LastChangeDate"];
                var lastChangedId = helloAgain["System.VersionControl.LastChangeID"];

                var configurationFileLocation = GetConfigurationFileIfExists(existingRepositoryLocation);

                _dataAccessService.MakeRepository(Path.GetFileName(existingRepositoryLocation), existingRepositoryLocation, configurationFileLocation, urlOfRepo, Guid.Parse(extensionCLSID));
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

    private async Task<List<RepositoryManagementItemViewModel>> ConvertToLineItems(List<Repository> repositories)
    {
        _log.Information("Converting repositories from the database into view models for display");
        List<RepositoryManagementItemViewModel> items = [];

        foreach (var repo in repositories)
        {
            // Get url and branch name
            var thisRepoOfSorts = new LibGit2Sharp.Repository(repo.RepositoryClonePath);
            var localBranch = thisRepoOfSorts.Head.FriendlyName;

            // get most recent commit information
            var blah = new SourceControlProvider();

            // Will throw if the repository is not enhanced
            // Should this enhance here?
            IPerFolderRootPropertyProvider thisProvider = null;
            try
            {
                thisProvider = blah.GetProvider(repo.RepositoryClonePath);
            }
            catch
            {
                _log.Information("Exception when getting the provider.  Will try and make the repository enhanced");
            }

            var foundSourceControlProvider = false;
            if (thisProvider == null)
            {
                _sourceControlRegistrar.AddRepositoryAlreadyOnMachine(repo.RepositoryClonePath);
                var extensionCLSID = string.Empty;
                IExtensionWrapper selectedExtension = null;
                foreach (var extension in _repositorySourceControlProviders)
                {
                    extensionCLSID = extension?.ExtensionClassId ?? string.Empty;
                    var didAdd = await _sourceControlRegistrar.AssignSourceControlProviderToRepository(extension, repo.RepositoryClonePath);
                    if (didAdd.Result == Customization.Helpers.ResultType.Success)
                    {
                        foundSourceControlProvider = true;
                        selectedExtension = extension;
                        break;
                    }
                }
            }

            if (foundSourceControlProvider)
            {
                thisProvider = blah.GetProvider(repo.RepositoryClonePath);
            }

            RepositoryManagementItemViewModel lineItem = null;
            if (thisProvider != null)
            {
                var helloAgain = thisProvider.GetProperties(_properties, string.Empty);
                var authorName = helloAgain["System.VersionControl.LastChangeAuthorName"];
                var lastChangedDate = helloAgain["System.VersionControl.LastChangeDate"];
                var lastChangedId = helloAgain["System.VersionControl.LastChangeID"];

                lineItem = _factory.MakeViewModel(repo.RepositoryName, repo.RepositoryClonePath, repo.IsHidden);
                lineItem.Branch = localBranch;
                lineItem.LatestCommit = $"{lastChangedId}*{authorName}*{lastChangedDate}";
            }
            else
            {
                lineItem = _factory.MakeViewModel(repo.RepositoryName, repo.RepositoryClonePath, repo.IsHidden);
                lineItem.Branch = string.Empty;
                lineItem.LatestCommit = string.Empty;
            }

            lineItem.HasAConfigurationFile = repo.HasAConfigurationFile;
            items.Add(lineItem);
        }

        return items;
    }

    private string GetConfigurationFileIfExists(string repositoryPath)
    {
        var configurationDirectory = Path.Join(repositoryPath, DscHelpers.ConfigurationFolderName);
        if (Directory.Exists(configurationDirectory))
        {
            var fileToUse = Directory.EnumerateFiles(configurationDirectory)
            .Where(file => file.EndsWith(DscHelpers.ConfigurationFileYamlExtension, StringComparison.OrdinalIgnoreCase) ||
                           file.EndsWith(DscHelpers.ConfigurationFileWingetExtension, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(configurationFile => File.GetLastWriteTime(configurationFile))
            .FirstOrDefault();

            if (fileToUse != null)
            {
                return fileToUse;
            }
        }

        return string.Empty;
    }
}
