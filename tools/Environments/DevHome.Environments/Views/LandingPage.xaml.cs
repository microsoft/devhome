// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using DevHome.Common;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Environments.Helpers;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Views;

public sealed partial class LandingPage : ToolPage
{
    public override string ShortName => "Environments";

    public LandingPageViewModel ViewModel { get; }

    public IWindowsIdentityService IdentityService { get; }

    public ToastNotificationService NotificationService { get; }

    public LandingPage()
    {
        ViewModel = Application.Current.GetService<LandingPageViewModel>();

        // To Do: Move this to a view model
        IdentityService = Application.Current.GetService<IWindowsIdentityService>();
        NotificationService = Application.Current.GetService<ToastNotificationService>();

        InitializeComponent();
        CheckIfUserIsAHyperVAdmin();

#if DEBUG
        Loaded += AddDebugButtons;
#endif
    }

    private void CheckIfUserIsAHyperVAdmin()
    {
        if (!IdentityService.IsUserHyperVAdmin())
        {
            NotificationService.ShowHyperVAdminWarningToast();
        }
#if DEBUG
        NotificationService.ShowSimpleToast(message: "You are viewing the Environments Page", title: "Welcome");
#endif
    }

#if DEBUG
    private void AddDebugButtons(object sender, RoutedEventArgs e)
    {
        var onlyLocalButton = new Button
        {
            Content = "Load local testing values",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(3, 0, 0, 0),
        };
        onlyLocalButton.Click += LocalLoadButton_Click;
        SyncButtonGrid.Children.Add(onlyLocalButton);

        var onlyRemoteButton = new Button
        {
            Content = "Load real extension values",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(3, 0, 0, 0),
        };
        onlyRemoteButton.Click += RemoteLoadButton_Click;
        SyncButtonGrid.Children.Add(onlyRemoteButton);

        var column = Grid.GetColumn(Titlebar);
        Grid.SetColumn(onlyLocalButton, column + 1);
        Grid.SetColumn(onlyRemoteButton, column + 2);
    }

    private void LocalLoadButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadModel(true);
    }

    private void RemoteLoadButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadModel(false);
    }
#endif
}
