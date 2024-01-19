// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.CustomControls;
public sealed partial class CardBody : UserControl
{
    public const string DefaultCardBodyImagePath = "ms-appx:///DevHome.Common/Environments/Assets/EnvironmentsDefaultWallpaper.png";

    public CardBody()
    {
        this.InitializeComponent();
    }

    public DataTemplate ActionControlTemplate
    {
        get => (DataTemplate)GetValue(ActionControlTemplateProperty);
        set => SetValue(ActionControlTemplateProperty, value);
    }

    public string ComputeSystemTitle
    {
        get => (string)GetValue(ComputeSystemTitleProperty);
        set => SetValue(ComputeSystemTitleProperty, value);
    }

    public string ComputeSystemAlternativeTitle
    {
        get => (string)GetValue(ComputeSystemAlternativeTitleProperty);
        set => SetValue(ComputeSystemAlternativeTitleProperty, value);
    }

    public BitmapImage ComputeSystemImage
    {
        get => (BitmapImage)GetValue(ComputeSystemImageProperty);
        set => SetValue(ComputeSystemImageProperty, value);
    }

    public CardStateColor StateColor
    {
        get => (CardStateColor)GetValue(StateColorProperty);
        set => SetValue(StateColorProperty, value);
    }

    public ComputeSystemState CardState
    {
        get => (ComputeSystemState)GetValue(CardStateProperty);
        set => SetValue(CardStateProperty, value);
    }

    public ObservableCollection<ICardProperty> ComputeSystemProperties
    {
        get => (ObservableCollection<ICardProperty>)GetValue(ComputeSystemPropertiesProperty);
        set => SetValue(ComputeSystemPropertiesProperty, value);
    }

    public DataTemplate ComputeSystemPropertyTemplate
    {
        get => (DataTemplate)GetValue(ComputeSystemPropertyTemplateProperty);
        set => SetValue(ComputeSystemPropertyTemplateProperty, value);
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemTitleProperty = DependencyProperty.Register(nameof(ComputeSystemTitle), typeof(string), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemAlternativeTitleProperty = DependencyProperty.Register(nameof(ComputeSystemAlternativeTitle), typeof(string), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty StateColorProperty = DependencyProperty.Register(nameof(StateColor), typeof(CardStateColor), typeof(CardBody), new PropertyMetadata(CardStateColor.Neutral));
    private static readonly DependencyProperty CardStateProperty = DependencyProperty.Register(nameof(CardState), typeof(ComputeSystemState), typeof(CardBody), new PropertyMetadata(ComputeSystemState.Unknown));
    private static readonly DependencyProperty ComputeSystemImageProperty = DependencyProperty.Register(nameof(ComputeSystemImage), typeof(BitmapImage), typeof(CardBody), new PropertyMetadata(new BitmapImage { UriSource = new Uri(DefaultCardBodyImagePath), }));
    private static readonly DependencyProperty ComputeSystemPropertiesProperty = DependencyProperty.Register(nameof(ComputeSystemProperties), typeof(ObservableCollection<ICardProperty>), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemPropertyTemplateProperty = DependencyProperty.Register(nameof(ComputeSystemPropertyTemplate), typeof(DataTemplate), typeof(CardBody), new PropertyMetadata(null));
}
