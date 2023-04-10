// Copyright (c) Microsoft Corporation and Contributors.
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

    public object PreviousTemplate
    {
        get => GetValue(PreviousTemplateProperty);
        set => SetValue(PreviousTemplateProperty, value);
    }

    public object NextTemplate
    {
        get => GetValue(NextTemplateProperty);
        set => SetValue(NextTemplateProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(nameof(ContentTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty CancelTemplateProperty = DependencyProperty.Register(nameof(CancelTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty PreviousTemplateProperty = DependencyProperty.Register(nameof(PreviousTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
    public static readonly DependencyProperty NextTemplateProperty = DependencyProperty.Register(nameof(NextTemplate), typeof(object), typeof(SetupFlowNavigation), new PropertyMetadata(null));
}
