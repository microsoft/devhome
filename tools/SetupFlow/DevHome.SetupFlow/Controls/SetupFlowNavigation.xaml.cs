// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Controls;

public sealed partial class SetupFlowNavigation : UserControl
{
    public SetupFlowNavigation()
    {
        this.InitializeComponent();
    }

    public object ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public object CancelTemplate
    {
        get => GetValue(CancelTemplateProperty);
        set => SetValue(CancelTemplateProperty, value);
    }

    public Visibility CancelVisibility
    {
        get => (Visibility)GetValue(CancelVisibilityProperty);
        set => SetValue(CancelVisibilityProperty, value);
    }

    public object PreviousTemplate
    {
        get => GetValue(PreviousTemplateProperty);
        set => SetValue(PreviousTemplateProperty, value);
    }

    public Visibility PreviousVisibility
    {
        get => (Visibility)GetValue(PreviousVisibilityProperty);
        set => SetValue(PreviousVisibilityProperty, value);
    }

    public object NextTemplate
    {
        get => GetValue(NextTemplateProperty);
        set => SetValue(NextTemplateProperty, value);
    }

    public Visibility NextVisibility
    {
        get => (Visibility)GetValue(NextVisibilityProperty);
        set => SetValue(NextVisibilityProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(nameof(ContentTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty CancelTemplateProperty = DependencyProperty.Register(nameof(CancelTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty CancelVisibilityProperty = DependencyProperty.Register(nameof(CancelVisibility), typeof(Visibility), typeof(SetupFlowNavigation), new PropertyMetadata(Visibility.Visible));
    public static readonly DependencyProperty PreviousTemplateProperty = DependencyProperty.Register(nameof(PreviousTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty PreviousVisibilityProperty = DependencyProperty.Register(nameof(PreviousVisibility), typeof(Visibility), typeof(SetupFlowNavigation), new PropertyMetadata(Visibility.Visible));
    public static readonly DependencyProperty NextTemplateProperty = DependencyProperty.Register(nameof(NextTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty NextVisibilityProperty = DependencyProperty.Register(nameof(NextVisibility), typeof(Visibility), typeof(SetupFlowNavigation), new PropertyMetadata(Visibility.Visible));
}
