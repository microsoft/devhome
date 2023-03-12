// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUIEx;
using static DevHome.SetupFlow.RepoConfig.Models.Common;

namespace DevHome.SetupFlow.RepoConfig.ViewModels;
public class AddRepoViewModel
{
    private RepositoryProviders _providers;

    public async Task StartPlugins()
    {
        var pluginService = Application.Current.GetService<IPluginService>();
        var pluginWrappers = await pluginService.GetInstalledPluginsAsync();
        var localProviders = new List<IPlugin>();
        foreach (var pluginWrapper in pluginWrappers.Where(
            plugin => plugin.HasProviderType(ProviderType.Repository) &&
            plugin.HasProviderType(ProviderType.DevId)))
        {
            await pluginWrapper.StartPlugin();
            var repositoryProvider = pluginWrapper?.GetPluginObject();
            if (repositoryProvider == null)
            {
                continue;
            }

            localProviders.Add(repositoryProvider);
        }

        _providers = new RepositoryProviders(localProviders);
    }

    /// <summary>
    /// Logs a user into a provider.
    /// </summary>
    /// <param name="providerName">The name of the provider.  This should match IRepositoryProvider.GetDisplayName</param>
    public void LogIntoProvider(string providerName)
    {
        _providers.LogInToProvider(providerName);
    }

    /// <summary>
    /// Gets all logged in accounts in the specified provider.
    /// </summary>
    /// <param name="providerName">The name of the provider.  This should match IRepositoryProvider.GetDisplayName</param>
    /// <returns>A list of all logged in accounts.  Can be empty.</returns>
    public IEnumerable<IDeveloperId> GetAllLoggedInAccounts(string providerName)
    {
        return _providers.GetAllLoggedInAccounts(providerName);
    }

    /// <summary>
    /// Gets all the names of all providers Dev Home can find.
    /// </summary>
    /// <returns>All the results from IRepository.GetDisplayName</returns>
    public IEnumerable<string> QueryForAllProviderNames()
    {
        return _providers.GetAllProviderNames();
    }

    /// <summary>
    /// Gets all dev volumes currently on the users machine and any dev volumes they opted to create.
    /// </summary>
    /// <returns>A list of dev volume locations(?)  Dunno.  Need to wait for Branden</returns>
    public List<string> QueryForNewAndExistingDevVolumes()
    {
        // Somehow search for
        // 1. Existing DevVolumes on the system, and
        // 2. Dev volumes the user has said they want to add in a previous step.
        return new List<string>();
    }

    /// <summary>
    /// Opens the directory picker
    /// </summary>
    /// <returns>null if the user closed the dialog without picking a location.  Otherwise the location they chose.</returns>
    public async Task<DirectoryInfo> PickCloneDirectoryAsync()
    {
        var folderPicker = new FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Application.Current.GetService<WindowEx>().GetWindowHandle());
        folderPicker.FileTypeFilter.Add("*");

        var locationToCloneTo = await folderPicker.PickSingleFolderAsync();
        if (locationToCloneTo != null && locationToCloneTo.Path.Length > 0)
        {
            return new DirectoryInfo(locationToCloneTo.Path);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all the repositories for the the specified provider and account.
    /// </summary>
    /// <param name="repositoryProvider">The provider.  This should match IRepositoryProvider.GetDisplayName</param>
    /// <param name="developerId">The account that owns the repositories.</param>
    /// <returns>A list of all repositories the account has for the provider.</returns>
    public async Task<IEnumerable<IRepository>> GetRepositoriesAsync(string repositoryProvider, IDeveloperId developerId)
    {
        return await _providers.GetAllRepositoriesAsync(repositoryProvider, developerId);
    }
}
