// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Controls;

public sealed partial class ProgressTextRing : UserControl, INotifyPropertyChanged
{
    public ProgressTextRing()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ProgressTextRing), new PropertyMetadata(false, OnIsActivePropertyChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static void OnIsActivePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProgressTextRing)d;
        control.OnPropertyChanged(nameof(TextBlockVisibility));
    }

    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register(nameof(Diameter), typeof(double), typeof(ProgressTextRing), new PropertyMetadata(0.0, OnDiameterPropertyChanged));

    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    private static void OnDiameterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProgressTextRing)d;
        control.OnPropertyChanged(nameof(TextBlockFontSize));
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressTextRing), new PropertyMetadata(0.0, OnValuePropertyChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ProgressTextRing)d;
        control.OnPropertyChanged(nameof(PercentageText));
    }

    private Visibility TextBlockVisibility => IsActive ? Visibility.Visible : Visibility.Collapsed;

    private double TextBlockFontSize => FontSize > 0 ? FontSize : Diameter / 3.6;

    public string PercentageText => $"{Value}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
