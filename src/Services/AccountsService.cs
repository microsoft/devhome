// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Logging;
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
            var developerIdsResult = devIdProvider.GetLoggedInDeveloperIds();
            if (developerIdsResult.Result.Status == ProviderOperationStatus.Failure)
            {
                GlobalLog.Logger?.ReportError($"{developerIdsResult.Result.DisplayMessage} - {developerIdsResult.Result.DiagnosticText}");
                return;
            }

            var devIds = developerIdsResult.DeveloperIds.ToList();

            TelemetryFactory.Get<ITelemetry>().Log("Startup_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.DisplayName, devIds));

            devIdProvider.Changed += ChangedEventHandler;
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

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider)
    {
        var developerIdsResult = iDevIdProvider.GetLoggedInDeveloperIds();
        if (developerIdsResult.Result.Status == ProviderOperationStatus.Failure)
        {
            GlobalLog.Logger?.ReportError($"{developerIdsResult.Result.DisplayMessage} - {developerIdsResult.Result.DiagnosticText}");
            return (IReadOnlyList<IDeveloperId>)Enumerable.Empty<IDeveloperId>();
        }

        return developerIdsResult.DeveloperIds.ToList();
    }

    // public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider) => iDevIdProvider.GetLoggedInDeveloperIds().ToList();
    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IExtension extension)
    {
        if (extension.GetProvider(ProviderType.DeveloperId) is IDeveloperIdProvider devIdProvider)
        {
            return GetDeveloperIds(devIdProvider);
        }

        return new List<IDeveloperId>();
    }

    public void ChangedEventHandler(object? sender, IDeveloperId developerId)
    {
        if (sender is IDeveloperIdProvider devIdProvider)
        {
            var authenticationState = devIdProvider.GetDeveloperIdState(developerId);

            if (authenticationState == AuthenticationState.LoggedIn)
            {
                TelemetryFactory.Get<ITelemetry>().Log("Login_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.DisplayName, developerId));

                // Bring focus back to DevHome after login
                _ = PInvoke.SetForegroundWindow((HWND)Process.GetCurrentProcess().MainWindowHandle);
            }
            else if (authenticationState == AuthenticationState.LoggedOut)
            {
                TelemetryFactory.Get<ITelemetry>().Log("Logout_DevId_Event", LogLevel.Critical, new DeveloperIdEvent(devIdProvider.DisplayName, developerId));
            }
        }
    }
}
