// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.Environments.Views;

public sealed partial class LandingPage : ToolPage
{
    public override string ShortName => "Environments";

    public LandingPageViewModel ViewModel { get; }

    public LandingPage()
    {
        ViewModel = Application.Current.GetService<LandingPageViewModel>();
        InitializeComponent();
        ViewModel.Initialize(NotificationQueue);

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

        TitleGrid.Children.Add(onlyLocalButton);

        var column = Grid.GetColumn(Titlebar);
        Grid.SetColumn(onlyLocalButton, column + 1);
    }

    [RelayCommand]
    private async Task LocalLoadButton()
    {
        await ViewModel.LoadModelAsync(true);
    }
#endif

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.LoadModelAsync(false);

        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(EnvironmentLandingPage);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
