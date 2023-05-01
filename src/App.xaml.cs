// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Activation;
using DevHome.Common.Contracts;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.Services;
using DevHome.Settings.Extensions;
using DevHome.SetupFlow.Extensions;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using DevHome.ViewModels;
using DevHome.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevHome;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application, IApp
{
    private readonly DispatcherQueue _dispatcherQueue;

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

    internal static NavConfig NavConfig { get; } = System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "navConfig.json")), SourceGenerationContext.Default.NavConfig)!;

    public App()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<Microsoft.UI.Xaml.LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, ProtocolActivationHandler>();

            // Services
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPluginService, PluginService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IAccountsService, AccountsService>();
            services.AddSingleton<IInfoBarService, InfoBarService>();
            services.AddSingleton<IAppInfoService, AppInfoService>();
            services.AddSingleton<ILogger>(LoggerFactory.Get<ILogger>());
            services.AddSingleton<IStringResource, StringResource>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Main window: Allow access to the main window
            // from anywhere in the application.
            services.AddSingleton<WindowEx>(_ => MainWindow);

            // Views and ViewModels
            services.AddTransient<FeedbackViewModel>();
            services.AddTransient<FeedbackPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<InitializationPage>();
            services.AddTransient<ShellViewModel>();
            services.AddTransient<WhatsNewViewModel>();
            services.AddTransient<InitializationViewModel>();

            // Settings
            services.AddSettings(context);

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

            // Setup flow
            services.AddSetupFlow(context);
        }).
        Build();

        UnhandledException += App_UnhandledException;
        AppInstance.GetCurrent().Activated += OnActivated;
    }

    private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        await GetService<IPluginService>().SignalStopPluginsAsync();
    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await GetService<IActivationService>().ActivateAsync(AppInstance.GetCurrent().GetActivatedEventArgs().Data);
        await GetService<IAccountsService>().InitializeAsync();

        await GetService<CatalogProvider>().InitializeAsync();
        await Task.Run(async () => await GetService<IWindowsPackageManager>().ConnectToAllCatalogsAsync());
        await Task.Run(async () => await GetService<CatalogProvider>().LoadCatalogsAsync());
    }

    private void OnActivated(object? sender, AppActivationArguments args)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await GetService<IActivationService>().ActivateAsync(args.Data);
        });
    }
}
