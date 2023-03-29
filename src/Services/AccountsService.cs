// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Contracts.Services;
using DevHome.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public AccountsService()
    {
    }

    public async Task InitializeAsync()
    {
        (await GetDevIdProviders()).ToList().ForEach((devIdProvider) =>
        {
            var devIds = devIdProvider.GetLoggedInDeveloperIds().ToList();

            LoggingHelper.AccountStartupEvent("Startup_DevId_Event", devIdProvider.GetName(), devIds);

            devIdProvider.LoggedIn += LoggedInEventHandler;
            devIdProvider.LoggedOut += LoggedOutEventHandler;
        });
    }

    public async Task<IReadOnlyList<IDevIdProvider>> GetDevIdProviders()
    {
        var devIdProviders = new List<IDevIdProvider>();
        var pluginService = new PluginService();
        var plugins = await pluginService.GetInstalledPluginsAsync(ProviderType.DevId);
        foreach (var plugin in plugins)
        {
            var devIdProvider = await plugin.GetProviderAsync<IDevIdProvider>();
            if (devIdProvider is not null)
            {
                devIdProviders.Add(devIdProvider);
            }
        }

        return devIdProviders;
    }

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDevIdProvider iDevIdProvider) => iDevIdProvider.GetLoggedInDeveloperIds().ToList();

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IPlugin plugin)
    {
        if (plugin.GetProvider(ProviderType.DevId) is IDevIdProvider devIdProvider)
        {
            return GetDeveloperIds(devIdProvider);
        }

        return new List<IDeveloperId>();
    }

    public void LoggedInEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider devIdProvider)
        {
            LoggingHelper.AccountEvent("Login_DevId_Event", devIdProvider.GetName(), developerId.LoginId());
        }

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDevIdProvider devIdProvider)
        {
            LoggingHelper.AccountEvent("Logout_DevId_Event", devIdProvider.GetName(), developerId.LoginId());
        }
    }
}
