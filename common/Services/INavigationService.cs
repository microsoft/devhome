// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.Common.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack
    {
        get;
    }

    // Used to pass data between view models during a navigation
    object? LastParameterUsed
    {
        get;
    }

    Frame? Frame
    {
        get; set;
    }

    string DefaultPage
    {
        get; set;
    }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();

    bool GoForward();
}

// Expose known page keys so that a project doesn't need to include a ProjectReference to another project
// just to navigate to another page.
public static class KnownPageKeys
{
    public static readonly string Dashboard = "DevHome.Dashboard.ViewModels.DashboardViewModel";
    public static readonly string Extensions = "DevHome.ExtensionLibrary.ViewModels.ExtensionLibraryViewModel";
    public static readonly string WhatsNew = "DevHome.ViewModels.WhatsNewViewModel";
    public static readonly string Settings = "DevHome.Settings.ViewModels.SettingsViewModel";
    public static readonly string Feedback = "DevHome.Settings.ViewModels.FeedbackViewModel";
    public static readonly string Environments = "DevHome.Environments.ViewModels.LandingPageViewModel";
    public static readonly string SetupFlow = "DevHome.SetupFlow.ViewModels.SetupFlowViewModel";

    // Will not work with navigation service nativly.  Used for the dictionary in SetupFlowViewModel
    public static readonly string RepositoryConfiguration = "RepositoryConfiguration";
}
