// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;

[INotifyPropertyChanged]
public sealed partial class WindowTitleBar : UserControl
{
    private string TitleString { get; set; } = string.Empty;

    public object Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IconElement Icon
    {
        get => (IconElement)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool HideIcon
    {
        get => (bool)GetValue(HideIconProperty);
        set => SetValue(HideIconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public WindowTitleBar()
    {
        this.InitializeComponent();
    }

    private static void OnTitleChanged(WindowTitleBar windowTitleBar, object newValue)
    {
        if (newValue is string title)
        {
            windowTitleBar.TitleString = title;
            windowTitleBar.OnPropertyChanged(nameof(windowTitleBar.TitleString));
            windowTitleBar.TitleControl.Content = windowTitleBar.DefaultTitleContent;
        }
        else
        {
            windowTitleBar.TitleControl.Content = newValue;
        }
    }

    private static void OnIconChanged(WindowTitleBar windowTitleBar, IconElement newValue)
    {
        windowTitleBar.IconControl.Content = newValue ?? windowTitleBar.DefaultIconContent;
    }

    private static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(object), typeof(WindowTitleBar), new PropertyMetadata(null, (s, a) => OnTitleChanged((WindowTitleBar)s, a.NewValue)));
    private static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(WindowTitleBar), new PropertyMetadata(null, (s, a) => OnIconChanged((WindowTitleBar)s, (IconElement)a.NewValue)));
    private static readonly DependencyProperty HideIconProperty = DependencyProperty.Register(nameof(HideIcon), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(false));
    private static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(true));
}
