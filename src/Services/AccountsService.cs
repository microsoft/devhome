// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    public AccountsService()
    {
    }

    public async Task InitializeAsync()
    {
        (await GetDevIdProviders()).ToList().ForEach((devIdProvider) =>
        {
            var devIds = devIdProvider.GetLoggedInDeveloperIds().ToList();

            TelemetryFactory.Get<ITelemetry>().Log("Startup_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), devIds));

            devIdProvider.LoggedIn += LoggedInEventHandler;
            devIdProvider.LoggedOut += LoggedOutEventHandler;
        });
    }

    public async Task<IReadOnlyList<IDeveloperIdProvider>> GetDevIdProviders()
    {
        var devIdProviders = new List<IDeveloperIdProvider>();
        var extensionService = Application.Current.GetService<IExtensionService>();
        var extensions = await extensionService.GetInstalledExtensionsAsync(ProviderType.DeveloperId);

        foreach (var extension in extensions)
        {
            var devIdProvider = await extension.GetProviderAsync<IDeveloperIdProvider>();
            if (devIdProvider is not null)
            {
                devIdProviders.Add(devIdProvider);
            }
        }

        return devIdProviders;
    }

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider) => iDevIdProvider.GetLoggedInDeveloperIds().ToList();

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
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryFactory.Get<ITelemetry>().Log("Login_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), developerId));
        }

        // Bring focus back to DevHome after login
        _ = PInvoke.SetForegroundWindow((HWND)Process.GetCurrentProcess().MainWindowHandle);
    }

    public void LoggedOutEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            TelemetryFactory.Get<ITelemetry>().Log("Logout_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.GetName(), developerId));
        }
    }
}
