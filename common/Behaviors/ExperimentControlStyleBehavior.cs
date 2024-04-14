// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace DevHome.Common.Behaviors;

public class ExperimentControlStyleBehavior : ExperimentControlBehavior
{
    public Style ExperimentStyle
    {
        get => (Style)GetValue(ExperimentStyleProperty);
        set => SetValue(ExperimentStyleProperty, value);
    }

    public static readonly DependencyProperty ExperimentStyleProperty = DependencyProperty.Register(nameof(ExperimentStyle), typeof(Style), typeof(ExperimentControlBehavior), new PropertyMetadata(null, OnExperimentChanged));

    protected override void OnExperimentControlUpdate(bool isExperimentEnabled)
    {
        if (isExperimentEnabled && AssociatedObject is FrameworkElement element && ExperimentStyle != null)
        {
            element.Style = ExperimentStyle;
        }
    }
}
