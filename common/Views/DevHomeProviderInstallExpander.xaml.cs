// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DevHome.Common.Environments.CustomControls;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace DevHome.Common.Views;

public sealed partial class DevHomeProviderInstallExpander : UserControl
{
    public DevHomeProviderInstallExpander()
    {
        this.InitializeComponent();
    }

    public string ExpanderTitle
    {
        get => (string)GetValue(ExpanderTitleProperty);
        set => SetValue(ExpanderTitleProperty, value);
    }

    public string ExpanderSubTitle
    {
        get => (string)GetValue(ExpanderSubTitleProperty);
        set => SetValue(ExpanderSubTitleProperty, value);
    }

    public string ExtensionDisplayName
    {
        get => (string)GetValue(ExtensionDisplayNameProperty);
        set => SetValue(ExtensionDisplayNameProperty, value);
    }

    public List<string> ProviderTypeTags
    {
        get => List<string>GetValue(ProviderTypeTagsProperty);
        set => SetValue(ProviderTypeTagsProperty, value);
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

    public List<CardProperty> ComputeSystemProperties
    {
        get => (ObservableCollection<CardProperty>)GetValue(ComputeSystemPropertiesProperty);
        set => SetValue(ComputeSystemPropertiesProperty, value);
    }

    public bool ShouldShowInDefiniteProgress
    {
        get => (bool)GetValue(ShouldShowInDefiniteProgressProperty);
        set => SetValue(ShouldShowInDefiniteProgressProperty, value);
    }

    public DataTemplate ComputeSystemPropertyTemplate
    {
        get => (DataTemplate)GetValue(ComputeSystemPropertyTemplateProperty);
        set => SetValue(ComputeSystemPropertyTemplateProperty, value);
    }

    private static void OnCardBodyChanged(CardBody cardBody, BitmapImage args)
    {
        if (cardBody != null)
        {
            if (args == null)
            {
                cardBody.ComputeSystemImage = new BitmapImage(new Uri(DefaultCardBodyImagePath));
                return;
            }

            cardBody.ComputeSystemImage = args;
        }
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemTitleProperty = DependencyProperty.Register(nameof(ComputeSystemTitle), typeof(string), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemAlternativeTitleProperty = DependencyProperty.Register(nameof(ComputeSystemAlternativeTitle), typeof(string), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty StateColorProperty = DependencyProperty.Register(nameof(StateColor), typeof(CardStateColor), typeof(CardBody), new PropertyMetadata(CardStateColor.Neutral));
    private static readonly DependencyProperty CardStateProperty = DependencyProperty.Register(nameof(CardState), typeof(ComputeSystemState), typeof(CardBody), new PropertyMetadata(ComputeSystemState.Unknown));
    private static readonly DependencyProperty ComputeSystemImageProperty = DependencyProperty.Register(nameof(ComputeSystemImage), typeof(BitmapImage), typeof(CardBody), new PropertyMetadata(new BitmapImage { UriSource = new Uri(DefaultCardBodyImagePath) }, (s, e) => OnCardBodyChanged((CardBody)s, (BitmapImage)e.NewValue)));
    private static readonly DependencyProperty ComputeSystemPropertiesProperty = DependencyProperty.Register(nameof(ComputeSystemProperties), typeof(ObservableCollection<CardProperty>), typeof(CardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty ComputeSystemPropertyTemplateProperty = DependencyProperty.Register(nameof(ComputeSystemPropertyTemplate), typeof(DataTemplate), typeof(CardBody), new PropertyMetadata(null));
}
