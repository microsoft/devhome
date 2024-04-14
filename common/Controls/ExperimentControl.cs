// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Controls;

public sealed partial class ExperimentControl : ContentControl
{
    public ExperimentControl()
    {
        this.InitializeComponent();
    }

    public string? ExperimentKey
    {
        get => (string?)GetValue(ExperimentKeyProperty);
        set => SetValue(ExperimentKeyProperty, value);
    }

    public object DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    public object ExperimentContent
    {
        get => GetValue(ExperimentContentProperty);
        set => SetValue(ExperimentContentProperty, value);
    }

    public static readonly DependencyProperty ExperimentKeyProperty = DependencyProperty.Register(nameof(ExperimentKey), typeof(string), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));
    public static readonly DependencyProperty DefaultContentProperty = DependencyProperty.Register(nameof(DefaultContent), typeof(object), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));
    public static readonly DependencyProperty ExperimentContentProperty = DependencyProperty.Register(nameof(ExperimentContent), typeof(object), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));

    private static void ExperimentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        var control = (ExperimentControl)dependencyObject;
        control.Content = control.GetContent();
    }

    private object GetContent()
    {
        var experimentationService = Application.Current.GetService<IExperimentationService>();
        var isExperimentEnabled = !string.IsNullOrWhiteSpace(ExperimentKey) && experimentationService.IsExperimentEnabled(ExperimentKey);
        isExperimentEnabled = isExperimentEnabled && false;
        return isExperimentEnabled ? ExperimentContent : DefaultContent;
    }
}
