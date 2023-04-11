// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Helpers;
using Microsoft.UI.Xaml;
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

            TelemetryHelper.AccountStartupEvent("Startup_DevId_Event", devIdProvider.GetName(), devIds);

            devIdProvider.LoggedIn += LoggedInEventHandler;
            devIdProvider.LoggedOut += LoggedOutEventHandler;
        });
    }

    public async Task<IReadOnlyList<IDeveloperIdProvider>> GetDevIdProviders()
    {
        var devIdProviders = new List<IDeveloperIdProvider>();
        var pluginService = Application.Current.GetService<IPluginService>();
        var plugins = await pluginService.GetInstalledPluginsAsync(ProviderType.DeveloperId);

        foreach (var plugin in plugins)
        {
            var devIdProvider = await plugin.GetProviderAsync<IDeveloperIdProvider>();
            if (devIdProvider is not null)
            {
                devIdProviders.Add(devIdProvider);
            }
        }

        return devIdProviders;
    }

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider) => iDevIdProvider.GetLoggedInDeveloperIds().ToList();

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IPlugin plugin)
    {
        if (plugin.GetProvider(ProviderType.DeveloperId) is IDeveloperIdProvider devIdProvider)
        {
            return GetDeveloperIds(devIdProvider);
        }

        return new List<IDeveloperId>();
    }

    public void LoggedInEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryHelper.AccountEvent("Login_DevId_Event", devIdProvider.GetName(), developerId.LoginId());
        }

        // Bring focus back to DevHome after login
        SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryHelper.AccountEvent("Logout_DevId_Event", devIdProvider.GetName(), developerId.LoginId());
        }
    }
}
