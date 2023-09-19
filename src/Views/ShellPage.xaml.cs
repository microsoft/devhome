// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Common.Windows;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace DevHome.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel
    {
        get;
    }

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.NavigationService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);

        UpdateNavigationMenuItems();

        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        App.MainWindow.ExtendsContentIntoTitleBar = true;
        App.MainWindow.SetTitleBar(AppTitleBar);
        App.MainWindow.Activated += MainWindow_Activated;
        AppTitleBarText.Text = Application.Current.GetService<IAppInfoService>().GetAppNameLocalized();

        ActualThemeChanged += OnActualThemeChanged;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(App.MainWindow, ActualTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));

        await ViewModel.OnLoaded();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // Update the title bar if the system theme changes.
        TitleBarHelper.UpdateTitleBar(App.MainWindow, ActualTheme);
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        var resource = args.WindowActivationState == WindowActivationState.Deactivated ? "WindowCaptionForegroundDisabled" : "WindowCaptionForeground";

        AppTitleBarText.Foreground = (SolidColorBrush)App.Current.Resources[resource];
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.Margin = new Thickness()
        {
            Left = sender.CompactPaneLength,
            Top = AppTitleBar.Margin.Top,
            Right = AppTitleBar.Margin.Right,
            Bottom = AppTitleBar.Margin.Bottom,
        };

        ShellInfoBar.Margin = new Thickness()
        {
            Left = ShellInfoBar.Margin.Left,
            Top = sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 50 : 0,
            Right = ShellInfoBar.Margin.Right,
            Bottom = ShellInfoBar.Margin.Bottom,
        };
    }

    private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
    {
        var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

        if (modifiers.HasValue)
        {
            keyboardAccelerator.Modifiers = modifiers.Value;
        }

        keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

        return keyboardAccelerator;
    }

    private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var navigationService = Application.Current.GetService<INavigationService>();

        var result = navigationService.GoBack();

        args.Handled = result;
    }

    private void UpdateNavigationMenuItems()
    {
        foreach (var group in App.NavConfig.NavMenu.Groups)
        {
            foreach (var tool in group.Tools)
            {
                var navigationViewItemString = $@"
                    <NavigationViewItem
                        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        xmlns:helpers=""using:DevHome.Helpers""
                        x:Uid=""/{tool.Assembly}/Resources/NavigationPane""
                        helpers:NavigationHelper.NavigateTo=""{tool.ViewModelFullName}""
                        AutomationProperties.AutomationId=""{tool.Identity}"">
                        <NavigationViewItem.Icon>
                            <FontIcon FontFamily=""{{StaticResource SymbolThemeFontFamily}}"" Glyph=""&#x{tool.Icon};""/>
                        </NavigationViewItem.Icon>
                    </NavigationViewItem>";
                NavigationViewItem navigationViewItem = (NavigationViewItem)XamlReader.Load(navigationViewItemString);
                NavigationViewControl.MenuItems.Add(navigationViewItem);
            }
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var example = new Example2();
        example.Activate();
    }
}
