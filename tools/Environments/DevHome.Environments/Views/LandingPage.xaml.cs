// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
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

    public LandingPage()
    {
        ViewModel = Application.Current.GetService<LandingPageViewModel>();
        InitializeComponent();

#if DEBUG
        Loaded += AddDebugButtons;
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
            Command = LocalLoadButtonCommand,
        };

        SyncButtonGrid.Children.Add(onlyLocalButton);

        var onlyRemoteButton = new Button
        {
            Content = "Load real extension values",
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(3, 0, 0, 0),
            Command = RemoteLoadButtonCommand,
        };

        SyncButtonGrid.Children.Add(onlyRemoteButton);

        var column = Grid.GetColumn(Titlebar);
        Grid.SetColumn(onlyLocalButton, column + 1);
        Grid.SetColumn(onlyRemoteButton, column + 2);
    }

    [RelayCommand]
    private async Task LocalLoadButton()
    {
        await ViewModel.LoadModelAsync(true);
    }

    [RelayCommand]
    private async Task RemoteLoadButton()
    {
        await ViewModel.LoadModelAsync(false);
    }
#endif

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.HasPageLoadedForTheFirstTime)
        {
            return;
        }

        _ = ViewModel.LoadModelAsync(false);
    }
}
