// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    public AccountsService()
    {
    }

    public async Task InitializeAsync()
    {
        /*
        (await GetDevIdProviders()).ToList().ForEach((devIdProvider) =>
        {
            var devIds = devIdProvider.GetLoggedInDeveloperIds().ToList();

            TelemetryFactory.Get<ITelemetry>().Log("Startup_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), devIds));

            devIdProvider.LoggedIn += LoggedInEventHandler;
            devIdProvider.LoggedOut += LoggedOutEventHandler;
        });
        */
        await Task.Delay(2000);
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

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider) => throw new NotImplementedException();

    // public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider) => iDevIdProvider.GetLoggedInDeveloperIds().ToList();
    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IExtension extension)
    {
        if (extension.GetProvider(ProviderType.DeveloperId) is IDeveloperIdProvider devIdProvider)
        {
            return GetDeveloperIds(devIdProvider);
        }

        return new List<IDeveloperId>();
    }

    public void LoggedInEventHandler(object? sender, IDeveloperId developerId)
    {
        /*
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryFactory.Get<ITelemetry>().Log("Login_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), developerId));
        }

        // Bring focus back to DevHome after login
        _ = PInvoke.SetForegroundWindow((HWND)Process.GetCurrentProcess().MainWindowHandle);
        */
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        /*
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryFactory.Get<ITelemetry>().Log("Logout_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), developerId));
        }
        */
    }
}
