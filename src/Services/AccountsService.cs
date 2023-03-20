// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.Telemetry;
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

                iDevIdProvider.LoggedIn += OnLogin;
                iDevIdProvider.LoggedOut += OnLogout;
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

    public void OnLogin(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider iDevIdProvider)
        {
            _accountsDictionary[iDevIdProvider].Add(developerId);
            LoggingHelper.AccountEvent_Critical("LoggedInEvent", iDevIdProvider.GetName(), developerId.LoginId());
        }
        else
        {
            LoggerFactory.Get<ILogger>().LogException($"LoggedInEvent_InvalidObject_Failure", new InvalidComObjectException($"LoggedInEventHandler() sender: {sender?.ToString()} developerId: {developerId.LoginId}"));
        }
    }

    public void OnLogout(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider iDevIdProvider)
        {
            _accountsDictionary[iDevIdProvider].Remove(developerId);
            LoggingHelper.AccountEvent_Critical("LoggedOutEvent", iDevIdProvider.GetName(), developerId.LoginId());
        }
        else
        {
            LoggerFactory.Get<ILogger>().LogException($"LoggedOutEventHandler_InvalidObject_Failure", new InvalidComObjectException($"LoggedOutEventHandler() sender: {sender?.ToString()} developerId: {developerId.LoginId}"));
        }
    }
}
