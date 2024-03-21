// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.CustomControls;

public sealed partial class DevDriveCardBody : UserControl
{
    public DevDriveCardBody()
    {
        this.InitializeComponent();
    }

    public DataTemplate ActionControlTemplate
    {
        get => (DataTemplate)GetValue(ActionControlTemplateProperty);
        set => SetValue(ActionControlTemplateProperty, value);
    }

    public string DevDriveLabel
    {
        get => (string)GetValue(DevDriveLabelProperty);
        set => SetValue(DevDriveLabelProperty, value);
    }

    public ulong DevDriveSize
    {
        get => (ulong)GetValue(DevDriveSizeProperty);
        set => SetValue(DevDriveSizeProperty, value);
    }

    public ulong DevDriveFreeSize
    {
        get => (ulong)GetValue(DevDriveFreeSizeProperty);
        set => SetValue(DevDriveFreeSizeProperty, value);
    }

    public ulong DevDriveUsedSize
    {
        get => (ulong)GetValue(DevDriveUsedSizeProperty);
        set => SetValue(DevDriveUsedSizeProperty, value);
    }

    public double DevDriveFillPercentage
    {
        get => (double)GetValue(DevDriveFillPercentageProperty);
        set => SetValue(DevDriveFillPercentageProperty, value);
    }

    public string DevDriveUnitOfMeasure
    {
        get => (string)GetValue(DevDriveUnitOfMeasureProperty);
        set => SetValue(DevDriveUnitOfMeasureProperty, value);
    }

    public string DevDriveAlternativeLabel
    {
        get => (string)GetValue(DevDriveAlternativeLabelProperty);
        set => SetValue(DevDriveAlternativeLabelProperty, value);
    }

    public CardStateColor StateColor
    {
        get => (CardStateColor)GetValue(StateColorProperty);
        set => SetValue(StateColorProperty, value);
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveLabelProperty = DependencyProperty.Register(nameof(DevDriveLabel), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveSizeProperty = DependencyProperty.Register(nameof(DevDriveSize), typeof(ulong), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveFreeSizeProperty = DependencyProperty.Register(nameof(DevDriveFreeSize), typeof(ulong), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveUsedSizeProperty = DependencyProperty.Register(nameof(DevDriveUsedSize), typeof(ulong), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveFillPercentageProperty = DependencyProperty.Register(nameof(DevDriveFillPercentage), typeof(double), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveUnitOfMeasureProperty = DependencyProperty.Register(nameof(DevDriveUnitOfMeasure), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty DevDriveAlternativeLabelProperty = DependencyProperty.Register(nameof(DevDriveAlternativeLabel), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty StateColorProperty = DependencyProperty.Register(nameof(StateColor), typeof(CardStateColor), typeof(DevDriveCardBody), new PropertyMetadata(CardStateColor.Neutral));
}
