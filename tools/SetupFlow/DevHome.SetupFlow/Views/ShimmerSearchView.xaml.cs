// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

[INotifyPropertyChanged]
public sealed partial class ShimmerSearchView : UserControl
{
    [ObservableProperty]
    private IEnumerable<int> _shimmerPackages;

    /// <summary>
    /// Gets or sets the number of packages to display in a shimmer search
    /// </summary>
    public int PackageCount
    {
        get => (int)GetValue(PackageCountProperty);
        set => SetValue(PackageCountProperty, value);
    }

    public ShimmerSearchView()
    {
        this.InitializeComponent();
    }

    private void UpdateShimmerPackages()
    {
        ShimmerPackages = Enumerable.Range(1, PackageCount);
    }

    public static readonly DependencyProperty PackageCountProperty = DependencyProperty.Register(nameof(PackageCount), typeof(int), typeof(ShimmerSearchView), new PropertyMetadata(0, (c, _) => ((ShimmerSearchView)c).UpdateShimmerPackages()));
}
