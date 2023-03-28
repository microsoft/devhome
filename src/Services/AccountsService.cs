// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.ViewModels;
using Microsoft.Windows.DevHome.SDK;
using WinRT;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly Dictionary<IDevIdProvider, List<IDeveloperId>> _accountsDictionary;

    public AccountsService()
    {
        _accountsDictionary = new ();
    }

    public async Task InitializeAsync()
    {
        var pluginService = new PluginService();
        var plugins = await pluginService.GetInstalledPluginsAsync(ProviderType.DevId);
        foreach (var plugin in plugins)
        {
            var devIdProvider = await plugin.GetProviderAsync<IDevIdProvider>();

            if (devIdProvider is not null)
            {
                var devIds = devIdProvider.GetLoggedInDeveloperIds().ToList();
                _accountsDictionary.TryAdd(devIdProvider, devIds);

                LoggingHelper.AccountStartupEvent("Startup_DevId_Event", devIdProvider.GetName(), devIds);

                devIdProvider.LoggedIn += LoggedInEventHandler;
                devIdProvider.LoggedOut += LoggedOutEventHandler;
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
            LoggingHelper.AccountEvent("Login_DevId_Event", iDevIdProvider.GetName(), developerId.LoginId());
        }

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider iDevIdProvider)
        {
            _accountsDictionary[iDevIdProvider].Remove(developerId);
            LoggingHelper.AccountEvent("Logout_DevId_Event", iDevIdProvider.GetName(), developerId.LoginId());
        }
    }
}
