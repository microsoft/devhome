// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.System;

namespace DevHome.Views;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

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

        ActualThemeChanged += OnActualThemeChanged;

        PointerPressed += OnPointerPressed;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        TitleBarHelper.UpdateTitleBar(App.MainWindow, ActualTheme);

        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Right, VirtualKeyModifiers.Menu));
        KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoForward));

        await ViewModel.OnLoaded();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        // Update the title bar if the system theme changes.
        TitleBarHelper.UpdateTitleBar(App.MainWindow, ActualTheme);
        AppTitleBar.Repaint();

        ViewModel.NotifyActualThemeChanged();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        AppTitleBar.IsActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
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
        if ((sender.Key == VirtualKey.GoBack) ||
            ((sender.Key == VirtualKey.Left) && sender.Modifiers.HasFlag(VirtualKeyModifiers.Menu)))
        {
            args.Handled = Application.Current.GetService<INavigationService>().GoBack();
        }
        else if ((sender.Key == VirtualKey.GoForward) ||
            ((sender.Key == VirtualKey.Right) && sender.Modifiers.HasFlag(VirtualKeyModifiers.Menu)))
        {
            args.Handled = Application.Current.GetService<INavigationService>().GoForward();
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        var handled = false;

        // Handle mouse forward and back navigation.
        if (args.GetCurrentPoint(null).Properties.IsXButton1Pressed)
        {
            handled = Application.Current.GetService<INavigationService>().GoBack();
        }
        else if (args.GetCurrentPoint(null).Properties.IsXButton2Pressed)
        {
            handled = Application.Current.GetService<INavigationService>().GoForward();
        }

        args.Handled = handled;
    }

    public static readonly DependencyProperty ExperimentalFeatureProperty = DependencyProperty.Register(
        "ExperimentalFeature",
        typeof(ExperimentalFeature),
        typeof(ShellPage),
        new PropertyMetadata(null, OnExperimentalFeatureChanged));

    private static void OnExperimentalFeatureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue != null && e.NewValue is ExperimentalFeature experimentalFeature)
        {
            var navigationViewItem = (NavigationViewItem)d;
            navigationViewItem.Visibility = experimentalFeature.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static ExperimentalFeature? GetExperimentalFeature(NavigationViewItem navigationViewItem) => (ExperimentalFeature)navigationViewItem.GetValue(ExperimentalFeatureProperty);

    public void UpdateExperimentalPageState(ExperimentalFeature expFeature)
    {
        var nvis = NavigationViewControl.MenuItems.Where(s => GetExperimentalFeature((NavigationViewItem)s) == expFeature);
        foreach (NavigationViewItem nvi in nvis)
        {
            nvi.Visibility = expFeature.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateNavigationMenuItems()
    {
        var expService = App.Current.GetService<IExperimentationService>();
        foreach (var group in App.NavConfig.NavMenu.Groups)
        {
            foreach (var tool in group.Tools)
            {
                var expFeature = expService.ExperimentalFeatures.FirstOrDefault(x => x.Id == tool.ExperimentalFeatureIdentity);

                var navigationViewItemString = $@"
                    <NavigationViewItem
                        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        xmlns:helpers=""using:DevHome.Helpers""
                        xmlns:views=""using:DevHome.Views""
                        x:Uid=""/{tool.Assembly}/Resources/NavigationPane""
                        helpers:NavigationHelper.NavigateTo=""{tool.ViewModelFullName}""
                        AutomationProperties.AutomationId=""{tool.Identity}"">
                        <NavigationViewItem.Icon>
                            <FontIcon FontFamily=""{{StaticResource {tool.IconFontFamily}}}"" Glyph=""&#x{tool.Icon};""/>
                        </NavigationViewItem.Icon>
                    </NavigationViewItem>";
                var navigationViewItem = (NavigationViewItem)XamlReader.Load(navigationViewItemString);

                if (expFeature != null)
                {
                    navigationViewItem.Visibility = expFeature.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
                    expFeature.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ExperimentalFeature.IsEnabled))
                        {
                            UpdateExperimentalPageState(expFeature);
                        }
                    };
                }

                navigationViewItem.SetValue(ExperimentalFeatureProperty, expFeature);

                NavigationViewControl.MenuItems.Add(navigationViewItem);
            }
        }
    }
}
