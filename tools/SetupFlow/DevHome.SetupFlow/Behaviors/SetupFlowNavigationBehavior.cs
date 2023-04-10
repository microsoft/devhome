// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.SetupFlow.Behaviors;

/// <summary>
/// Behavior class for customizing the setup flow navigation content
/// </summary>
public class SetupFlowNavigationBehavior : Behavior<SetupFlowNavigation>
{
    private static SetupFlowNavigationBehavior _instance;

    protected override void OnAttached()
    {
        base.OnAttached();
        _instance = this;

        // Initialize to default template and values
        UpdateContentTemplate(_instance.DefaultContentTemplate);
        UpdateCancelTemplate(_instance.DefaultCancelTemplate);
        UpdatePreviousTemplate(_instance.DefaultPreviousTemplate);
        UpdateNextTemplate(_instance.DefaultNextTemplate);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _instance = null;
    }

    private static void SetTemporaryValue(UserControl control, DependencyProperty property, object value, Action defaultValueAction)
    {
        control.SetValue(property, value);
        control.Unloaded += (_, _) => defaultValueAction();
    }

    /***
     * Cancel
     */

    public object DefaultCancelTemplate { get; set; }

    public static object GetCancelTemplate(UserControl nav) => nav.GetValue(CancelTemplateProperty);

    public static void SetCancelTemplate(UserControl nav, object template) =>
        SetTemporaryValue(nav, CancelTemplateProperty, template, () => UpdateCancelTemplate(_instance?.DefaultCancelTemplate));

    public static void UpdateCancelTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.CancelTemplate = templateObject;
        }
    }

    public static readonly DependencyProperty CancelTemplateProperty = DependencyProperty.RegisterAttached("CancelTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateCancelTemplate(e.NewValue)));

    /***
     * Previous
     */

    public object DefaultPreviousTemplate { get; set; }

    public static object GetPreviousTemplate(UserControl nav) => nav.GetValue(PreviousTemplateProperty);

    public static void SetPreviousTemplate(UserControl nav, object template) =>
        SetTemporaryValue(nav, PreviousTemplateProperty, template, () => UpdatePreviousTemplate(_instance?.DefaultPreviousTemplate));

    public static void UpdatePreviousTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.PreviousTemplate = templateObject;
        }
    }

    public static readonly DependencyProperty PreviousTemplateProperty = DependencyProperty.RegisterAttached("PreviousTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdatePreviousTemplate(e.NewValue)));

    /***
     * Next
     */

    public object DefaultNextTemplate { get; set; }

    public static object GetNextTemplate(UserControl nav) => nav.GetValue(NextTemplateProperty);

    public static void SetNextTemplate(UserControl nav, object template) =>
        SetTemporaryValue(nav, NextTemplateProperty, template, () => UpdateNextTemplate(_instance?.DefaultNextTemplate));

    public static void UpdateNextTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.NextTemplate = templateObject;
        }
    }

    public static readonly DependencyProperty NextTemplateProperty = DependencyProperty.RegisterAttached("NextTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateNextTemplate(e.NewValue)));

    /***
     * Content
     */

    public object DefaultContentTemplate { get; set; }

    public static object GetContentTemplate(UserControl nav) => nav.GetValue(ContentTemplateProperty);

    public static void SetContentTemplate(UserControl nav, object template) =>
        SetTemporaryValue(nav, ContentTemplateProperty, template, () => UpdateContentTemplate(_instance?.DefaultContentTemplate));

    public static void UpdateContentTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.ContentTemplate = templateObject;
        }
    }

    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.RegisterAttached("ContentTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateContentTemplate(e.NewValue)));
}
