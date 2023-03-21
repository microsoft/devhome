// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Contracts.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    private readonly Dictionary<IDevIdProvider, List<IDeveloperId>> _accountsDictionary;

    public AccountsService()
    {
        _accountsDictionary = new ();
    }

    public async void InitializeAsync()
    {
        var pluginService = new PluginService();
        var plugins = pluginService.GetInstalledPluginsAsync(ProviderType.DevId).Result;
        foreach (var plugin in plugins)
        {
            if (!plugin.IsRunning())
            {
                await plugin.StartPlugin();
            }

            var pluginObj = plugin.GetPluginObject();
            var devIdProvider = pluginObj?.GetProvider(ProviderType.DevId);

            if (devIdProvider is IDevIdProvider iDevIdProvider)
            {
                _accountsDictionary.Add(iDevIdProvider, iDevIdProvider.GetLoggedInDeveloperIds().ToList());

                iDevIdProvider.LoggedIn += LoggedInEventHandler;
                iDevIdProvider.LoggedOut += LoggedOutEventHandler;
            }
        }
    }

    public IReadOnlyList<IDevIdProvider> GetDevIdProviders() => _accountsDictionary.Keys.ToList();

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDevIdProvider iDevIdProvider) => _accountsDictionary[iDevIdProvider];

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IPlugin plugin)
    {
        if (plugin.GetProvider(ProviderType.DevId) is IDevIdProvider iDevIdProvider)
        {
            return GetDeveloperIds(iDevIdProvider);
        }

        return new List<IDeveloperId>();
    }

    public void LoggedInEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider iDevIdProvider)
        {
            _accountsDictionary[iDevIdProvider].Add(developerId);
        }
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider iDevIdProvider)
        {
            _accountsDictionary[iDevIdProvider].Remove(developerId);
        }
    }
}
