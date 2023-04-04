// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;
public sealed partial class PackageView : UserControl
{
    public PackageViewModel Package
    {
        get => (PackageViewModel)GetValue(PackageProperty);
        set => SetValue(PackageProperty, value);
    }

    public PackageView()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty PackageProperty = DependencyProperty.Register(nameof(Package), typeof(PackageViewModel), typeof(PackageView), new PropertyMetadata(null));
}
