// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.Services;

public class AccountsService : IAccountsService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AccountsService));

    private readonly IExtensionService _extensionService;

    public AccountsService(IExtensionService extensionService)
    {
        _extensionService = extensionService;
    }

    public async Task InitializeAsync()
    {
        (await GetDevIdProviders()).ToList().ForEach((devIdProvider) =>
        {
            var developerIdsResult = devIdProvider.GetLoggedInDeveloperIds();
            if (developerIdsResult.Result.Status == ProviderOperationStatus.Failure)
            {
                _log.Error($"{developerIdsResult.Result.DisplayMessage} - {developerIdsResult.Result.DiagnosticText}");
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
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.DeveloperId);
        foreach (var extension in extensions)
        {
            try
            {
                var devIdProvider = await extension.GetProviderAsync<IDeveloperIdProvider>();
                if (devIdProvider is not null)
                {
                    devIdProviders.Add(devIdProvider);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to get {nameof(IDeveloperIdProvider)} provider from '{extension.PackageFamilyName}/{extension.ExtensionDisplayName}'");
            }
        }

        return devIdProviders;
    }

    public IReadOnlyList<IDeveloperId> GetDeveloperIds(IDeveloperIdProvider iDevIdProvider)
    {
        var developerIdsResult = iDevIdProvider.GetLoggedInDeveloperIds();
        if (developerIdsResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"{developerIdsResult.Result.DisplayMessage} - {developerIdsResult.Result.DiagnosticText}");
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
                TelemetryFactory.Get<ITelemetry>().Log("Login_DevId_Event", LogLevel.Critical, new DeveloperIdUserEvent(devIdProvider.DisplayName, developerId));

                // Bring focus back to DevHome after login
                _ = PInvoke.SetForegroundWindow((HWND)Process.GetCurrentProcess().MainWindowHandle);
            }
            else if (authenticationState == AuthenticationState.LoggedOut)
            {
                TelemetryFactory.Get<ITelemetry>().Log("Logout_DevId_Event", LogLevel.Critical, new DeveloperIdUserEvent(devIdProvider.DisplayName, developerId));
            }
        }
    }
}
