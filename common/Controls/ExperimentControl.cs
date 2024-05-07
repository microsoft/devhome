// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Controls;

/// <summary>
/// Control that displays content based on the state of an experiment.
/// </summary>
public sealed partial class ExperimentControl : ContentControl
{
    public ExperimentControl()
    {
        // Hide the control from the tab order.
        IsTabStop = false;
        Content = DefaultContent;
    }

    /// <summary>
    /// Gets or sets the key of the experiment to check for.
    /// </summary>
    public string? ExperimentKey
    {
        get => (string?)GetValue(ExperimentKeyProperty);
        set => SetValue(ExperimentKeyProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display when the experiment is disabled.
    /// </summary>
    public object DefaultContent
    {
        get => GetValue(DefaultContentProperty);
        set => SetValue(DefaultContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display when the experiment is enabled.
    /// </summary>
    public object ExperimentContent
    {
        get => GetValue(ExperimentContentProperty);
        set => SetValue(ExperimentContentProperty, value);
    }

    /// <summary>
    /// Handles the change to any of the dependency properties.
    /// </summary>
    /// <param name="dependencyObject">Experiment control.</param>
    /// <param name="dependencyPropertyChangedEventArgs">Event arguments.</param>
    private static void ExperimentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
    {
        var control = (ExperimentControl)dependencyObject;
        control.Content = control.GetContent();
    }

    /// <summary>
    /// Gets the content to display based on the experiment state.
    /// </summary>
    /// <returns>The content to display.</returns>
    private object GetContent()
    {
        if (string.IsNullOrEmpty(ExperimentKey))
        {
            return DefaultContent;
        }

        var experimentationService = Application.Current.GetService<IExperimentationService>();
        var isExperimentEnabled = experimentationService.IsExperimentEnabled(ExperimentKey);
        isExperimentEnabled |= true;
        return isExperimentEnabled ? ExperimentContent : DefaultContent;
    }

    // List of dependency properties.
    public static readonly DependencyProperty ExperimentKeyProperty = DependencyProperty.Register(nameof(ExperimentKey), typeof(string), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));
    public static readonly DependencyProperty DefaultContentProperty = DependencyProperty.Register(nameof(DefaultContent), typeof(object), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));
    public static readonly DependencyProperty ExperimentContentProperty = DependencyProperty.Register(nameof(ExperimentContent), typeof(object), typeof(ExperimentControl), new PropertyMetadata(null, ExperimentChanged));
}
