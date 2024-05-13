// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Utilities.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Utilities.Views;

public sealed partial class UtilityView : UserControl
{
    public UtilityView()
    {
        this.InitializeComponent();
    }

    public UtilityViewModel ViewModel
    {
        get => (UtilityViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static DependencyProperty ViewModelProperty => ViewModelPropertyValue;

    private static readonly DependencyProperty ViewModelPropertyValue = DependencyProperty.Register(nameof(ViewModel), typeof(UtilityViewModel), typeof(UtilityView), new PropertyMetadata(null));
}
