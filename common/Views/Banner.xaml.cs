// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;
public sealed partial class Banner : UserControl
{
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public int TextWidth
    {
        get => (int)GetValue(TextWidthProperty);
        set => SetValue(TextWidthProperty, value);
    }

    public string ImageSource
    {
        get => (string)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public ICommand ButtonCommand
    {
        get => (ICommand)GetValue(ButtonCommandProperty);
        set => SetValue(ButtonCommandProperty, value);
    }

    public bool HideButtonVisibility
    {
        get => (bool)GetValue(HideButtonVisibilityProperty);
        set => SetValue(HideButtonVisibilityProperty, value);
    }

    public ICommand HideButtonCommand
    {
        get => (ICommand)GetValue(HideButtonCommandProperty);
        set => SetValue(HideButtonCommandProperty, value);
    }

    public Banner()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register(nameof(TextWidth), typeof(int), typeof(Banner), new PropertyMetadata(null));
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ButtonTextProperty = DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(Banner), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register(nameof(ButtonCommand), typeof(ICommand), typeof(Banner), new PropertyMetadata(null));
    public static readonly DependencyProperty HideButtonVisibilityProperty = DependencyProperty.Register(nameof(HideButtonVisibility), typeof(bool), typeof(Banner), new PropertyMetadata(false));
    public static readonly DependencyProperty HideButtonCommandProperty = DependencyProperty.Register(nameof(HideButtonCommand), typeof(ICommand), typeof(Banner), new PropertyMetadata(null));
}
