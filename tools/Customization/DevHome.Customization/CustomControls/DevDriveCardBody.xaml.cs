// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.CustomControls;

public sealed partial class DevDriveCardBody : UserControl
{
    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty DevDriveLabelProperty = DependencyProperty.Register(nameof(DevDriveLabel), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty DevDriveFillPercentageProperty = DependencyProperty.Register(nameof(DevDriveFillPercentage), typeof(double), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty DevDriveAlternativeLabelProperty = DependencyProperty.Register(nameof(DevDriveAlternativeLabel), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty StateColorProperty = DependencyProperty.Register(nameof(StateColor), typeof(CardStateColor), typeof(DevDriveCardBody), new PropertyMetadata(CardStateColor.Neutral));

    private static readonly DependencyProperty DevDriveSizeTextProperty = DependencyProperty.Register(nameof(DevDriveSizeText), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty DevDriveUsedSizeTextProperty = DependencyProperty.Register(nameof(DevDriveUsedSizeText), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));

    private static readonly DependencyProperty DevDriveFreeSizeTextProperty = DependencyProperty.Register(nameof(DevDriveFreeSizeText), typeof(string), typeof(DevDriveCardBody), new PropertyMetadata(null));

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

    public double DevDriveFillPercentage
    {
        get => (double)GetValue(DevDriveFillPercentageProperty);
        set => SetValue(DevDriveFillPercentageProperty, value);
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

    public string DevDriveSizeText
    {
        get => (string)GetValue(DevDriveSizeTextProperty);
        set => SetValue(DevDriveSizeTextProperty, value);
    }

    public string DevDriveUsedSizeText
    {
        get => (string)GetValue(DevDriveUsedSizeTextProperty);
        set => SetValue(DevDriveUsedSizeTextProperty, value);
    }

    public string DevDriveFreeSizeText
    {
        get => (string)GetValue(DevDriveFreeSizeTextProperty);
        set => SetValue(DevDriveFreeSizeTextProperty, value);
    }
}
