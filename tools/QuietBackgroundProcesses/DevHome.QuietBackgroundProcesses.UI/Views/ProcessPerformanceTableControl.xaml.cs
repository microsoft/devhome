// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.QuietBackgroundProcesses.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.QuietBackgroundProcesses.UI.Views;

public sealed partial class ProcessPerformanceTableControl : UserControl
{
    public ObservableCollection<ProcessData> ProcessDatas { get; set; } = new ObservableCollection<ProcessData>();

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(object),
        typeof(AnalyticSummaryPopupViewModel),
        new PropertyMetadata(default));

    public object ItemsSource
    {
        get => (object)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ProcessPerformanceTableControl()
    {
        this.InitializeComponent();
    }
}
