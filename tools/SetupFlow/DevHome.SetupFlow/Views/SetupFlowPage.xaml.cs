// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement;
using DevHome.SetupFlow.AppManagement.Services;
using DevHome.SetupFlow.AppManagement.ViewModels;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Common.Services;
using DevHome.SetupFlow.ConfigurationFile.ViewModels;
using DevHome.SetupFlow.DevDrive;
using DevHome.SetupFlow.DevDrive.Services;
using DevHome.SetupFlow.DevDrive.ViewModels;
using DevHome.SetupFlow.Loading.ViewModels;
using DevHome.SetupFlow.MainPage.ViewModels;
using DevHome.SetupFlow.RepoConfig;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using DevHome.SetupFlow.Review.ViewModels;
using DevHome.SetupFlow.Summary.ViewModels;
using DevHome.SetupFlow.ViewModels;
using DevHome.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.Views;

public partial class SetupFlowPage : ToolPage
{
    private readonly IHost _host;

    public override string ShortName => "SetupFlow";

    public SetupFlowViewModel ViewModel { get; }

    public SetupFlowPage()
    {
        _host = CreateHost();
        ViewModel = _host.GetService<SetupFlowViewModel>();
        InitializeComponent();
    }

    private IHost CreateHost()
    {
        return Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Setup task groups
                services.AddTransient<DevDriveTaskGroup>();
                services.AddTransient<RepoConfigTaskGroup>();
                services.AddTransient<AppManagementTaskGroup>();
                services.AddTransient<ConfigurationFileTaskGroup>();

                // View-models: Setup flow page
                services.AddTransient<SetupFlowViewModel>();

                // View-models: Main page
                services.AddTransient<MainPageViewModel>();
                services.AddTransient<ConfigurationFileViewModel>();
                services.AddTransient<AddRepoViewModel>();

                // View-models: Dev Drive page
                services.AddTransient<DevDriveViewModel>();
                services.AddTransient<DevDriveReviewViewModel>();

                // View-models: Repo page
                services.AddTransient<RepoConfigViewModel>();
                services.AddTransient<RepoConfigReviewViewModel>();

                // View-models: Application management page
                services.AddTransient<SearchViewModel>();
                services.AddTransient<PackageViewModel>();
                services.AddTransient<PackageCatalogListViewModel>();
                services.AddTransient<AppManagementViewModel>();
                services.AddTransient<PackageCatalogViewModel>();
                services.AddTransient<AppManagementReviewViewModel>();

                // View-models: Review page
                services.AddTransient<ReviewViewModel>();
                services.AddTransient<LoadingViewModel>();

                // View-models: Loading page
                services.AddTransient<LoadingViewModel>();

                // View-models: Summary page
                services.AddTransient<SummaryViewModel>();

                // Services: Dev Home telemetry
                services.AddSingleton<ILogger>(LoggerFactory.Get<ILogger>());

                // Services: Dev Home common
                services.AddSingleton<IStringResource>(new StringResource($"{nameof(DevHome)}.{nameof(SetupFlow)}/Resources"));

                // Services: Setup flow common
                services.AddSingleton<SetupFlowOrchestrator>();

                // Services: App Management
                services.AddSingleton<IWindowsPackageManager, WindowsPackageManager>();
                services.AddSingleton<WindowsPackageManagerFactory>(new WindowsPackageManagerFactory(ClsidContext.Prod));
                services.AddSingleton<WinGetPackageJsonDataSource>();

                // Services: Dev Drive
                services.AddSingleton<IDevDriveManager, DevDriveManager>();
            })
            .Build();
    }
}
