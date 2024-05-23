// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.Common.Environments.CustomControls;

public sealed partial class CardHeader : UserControl
{
    public CardHeader()
    {
        this.InitializeComponent();
    }

    public DataTemplate ActionControlTemplate
    {
        get => (DataTemplate)GetValue(ActionControlTemplateProperty);
        set => SetValue(ActionControlTemplateProperty, value);
    }

    public string HeaderCaption
    {
        get => (string)GetValue(HeaderCaptionProperty);
        set => SetValue(HeaderCaptionProperty, value);
    }

    public bool OperationsVisibility
    {
        get => (bool)GetValue(OperationsVisibilityProperty);
        set => SetValue(OperationsVisibilityProperty, value);
    }

    public BitmapImage HeaderIcon
    {
        get => (BitmapImage)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(CardHeader), new PropertyMetadata(null));
    private static readonly DependencyProperty HeaderCaptionProperty = DependencyProperty.Register(nameof(HeaderCaption), typeof(string), typeof(CardHeader), new PropertyMetadata(null));
    private static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(nameof(HeaderIcon), typeof(BitmapImage), typeof(CardHeader), new PropertyMetadata(null));
    private static readonly DependencyProperty OperationsVisibilityProperty = DependencyProperty.Register(nameof(HeaderCaption), typeof(bool), typeof(CardHeader), new PropertyMetadata(null));
}
