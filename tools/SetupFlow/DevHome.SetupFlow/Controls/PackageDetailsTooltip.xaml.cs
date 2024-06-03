// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Controls;

public sealed partial class PackageDetailsTooltip : ToolTip
{
    public PackageViewModel Package
    {
        get => (PackageViewModel)GetValue(PackageProperty);
        set => SetValue(PackageProperty, value);
    }

    public PackageDetailsTooltip()
    {
        this.InitializeComponent();
    }

    private static readonly DependencyProperty PackageProperty = DependencyProperty.Register(nameof(Package), typeof(PackageViewModel), typeof(PackageDetailsTooltip), new PropertyMetadata(null));
}
