// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace DevHome.Common.Behaviors;

public abstract class ExperimentControlBehavior : Behavior
{
    public string ExperimentKey
    {
        get => (string)GetValue(ExperimentKeyProperty);
        set => SetValue(ExperimentKeyProperty, value);
    }

    public static readonly DependencyProperty ExperimentKeyProperty = DependencyProperty.Register(nameof(ExperimentKey), typeof(string), typeof(ExperimentControlBehavior), new PropertyMetadata(null, OnExperimentChanged));

    protected override void OnAttached()
    {
        base.OnAttached();
        UpdateExperimentControl();
    }

    protected abstract void OnExperimentControlUpdate(bool isExperimentEnabled);

    protected static void OnExperimentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = (ExperimentControlBehavior)d;
        behavior.UpdateExperimentControl();
    }

    private void UpdateExperimentControl()
    {
        var experimentationService = Application.Current.GetService<IExperimentationService>();
        var isExperimentEnabled = experimentationService.IsExperimentEnabled(ExperimentKey);
        OnExperimentControlUpdate(isExperimentEnabled);
    }
}
