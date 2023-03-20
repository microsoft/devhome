// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Activation;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Core.Contracts.Services;
using DevHome.Core.Services;
using DevHome.Helpers;
using DevHome.Models;
using DevHome.Services;
using DevHome.SetupFlow.Extensions;
using DevHome.ViewModels;
using DevHome.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Newtonsoft.Json;
using WinRT;

namespace DevHome;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application, IApp
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host
    {
        get;
    }

    public T GetService<T>()
        where T : class => Host.GetService<T>();

    public static WindowEx MainWindow { get; } = new MainWindow();

    internal static NavConfig NavConfig { get; } = JsonConvert.DeserializeObject<NavConfig>(File.ReadAllText(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "navConfig.json")))!;

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPluginService, PluginService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IAccountsService, AccountsService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Main window: Allow access to the main window
            // from anywhere in the application.
            services.AddSingleton<WindowEx>(_ => MainWindow);

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<AccountsPageViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<FeedbackViewModel>();
            services.AddTransient<FeedbackPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<WhatsNewViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            // Setup flow
            services.AddSetupFlow();
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await GetService<IActivationService>().ActivateAsync(args);
        GetService<IAccountsService>().InitializeAsync();
    }
}
