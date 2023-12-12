// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Activation;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Contracts.Services;
using DevHome.Views;
using Microsoft.UI.Xaml;

namespace DevHome.Services;

public class ActivationService : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;

    private bool _isInitialActivation = true;

    public ActivationService(
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalSettingsService localSettingsService)
    {
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        if (_isInitialActivation)
        {
            _isInitialActivation = false;

            // Execute tasks before activation.
            await InitializeAsync();

            // We can skip the initialization page if it's not our first run and we're on Windows 11.
            // If we're on Windows 10, we need to go to the initialization page to install the WidgetService if we don't have it already.
            var skipInitialization = await _localSettingsService.ReadSettingAsync<bool>(WellKnownSettingsKeys.IsNotFirstRun)
                && RuntimeHelper.RunningOnWindows11;

            // Set the MainWindow Content.
            App.MainWindow.Content = skipInitialization
                ? Application.Current.GetService<ShellPage>()
                : Application.Current.GetService<InitializationPage>();

            // Activate the MainWindow.
            App.MainWindow.Activate();

            // Execute tasks after activation.
            await StartupAsync();
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        // Subscribe to theme changes.
        _themeSelectorService.ThemeChanged += (_, theme) => App.MainWindow.SetRequestedTheme(theme);
        _themeSelectorService.SetRequestedTheme();
        await Task.CompletedTask;
    }
}
