// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Controls;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.SetupFlow.Behaviors;

/// <summary>
/// Behavior class for customizing the setup flow navigation
/// </summary>
public class SetupFlowNavigationBehavior : Behavior<SetupFlowNavigation>
{
    /// <summary>
    /// Singleton instance of this behavior class implemented by the setup flow
    /// root page <see cref="SetupFlowNavigation"/> element.
    /// </summary>
    private static SetupFlowNavigationBehavior _instance;

    /// <summary>
    /// Gets or sets the default template for the cancel control
    /// </summary>
    public object DefaultCancelTemplate { get; set; }

    /// <summary>
    /// Getter for the attached property <see cref="CancelTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Cancel object</returns>
    public static object GetCancelTemplate(UserControl control) => control.GetValue(CancelTemplateProperty);

    /// <summary>
    /// Setter for the attached property <see cref="CancelTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="template">Cancel object</param>
    public static void SetCancelTemplate(UserControl control, object template) => control.SetValue(CancelTemplateProperty, template);

    /// <summary>
    /// Getter for the attached property <see cref="CancelVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Cancel visibility</returns>
    public static Visibility GetCancelVisibility(UserControl control) => (Visibility)control.GetValue(CancelVisibilityProperty);

    /// <summary>
    /// Setter for the attached property <see cref="CancelVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="visibility">Cancel visibility</param>
    public static void SetCancelVisibility(UserControl control, Visibility visibility) => control.SetValue(CancelVisibilityProperty, visibility);

    /// <summary>
    /// Gets or sets the default template for the previous control
    /// </summary>
    public object DefaultPreviousTemplate { get; set; }

    /// <summary>
    /// Getter for the attached property <see cref="PreviousTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Previous object</returns>
    public static object GetPreviousTemplate(UserControl control) => control.GetValue(PreviousTemplateProperty);

    /// <summary>
    /// Setter for the attached property <see cref="PreviousTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="template">Previous object</param>
    public static void SetPreviousTemplate(UserControl control, object template) => control.SetValue(PreviousTemplateProperty, template);

    /// <summary>
    /// Getter for the attached property <see cref="PreviousVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Previous visibility</returns>
    public static Visibility GetPreviousVisibility(UserControl control) => (Visibility)control.GetValue(PreviousVisibilityProperty);

    /// <summary>
    /// Setter for the attached property <see cref="PreviousVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="visibility">Previous visibility</param>
    public static void SetPreviousVisibility(UserControl control, Visibility visibility) => control.SetValue(PreviousVisibilityProperty, visibility);

    /// <summary>
    /// Gets or sets the default template for the next control
    /// </summary>
    public object DefaultNextTemplate { get; set; }

    /// <summary>
    /// Getter for the attached property <see cref="NextTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Next object</returns>
    public static object GetNextTemplate(UserControl control) => control.GetValue(NextTemplateProperty);

    /// <summary>
    /// Setter for the attached property <see cref="NextTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="template">Next object</param>
    public static void SetNextTemplate(UserControl control, object template) => control.SetValue(NextTemplateProperty, template);

    /// <summary>
    /// Getter for the attached property <see cref="NextVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Next visibility</returns>
    public static Visibility GetNextVisibility(UserControl control) => (Visibility)control.GetValue(NextVisibilityProperty);

    /// <summary>
    /// Setter for the attached property <see cref="NextVisibilityProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <param name="visibility">Next visibility</param>
    public static void SetNextVisibility(UserControl control, Visibility visibility) => control.SetValue(NextVisibilityProperty, visibility);

    /// <summary>
    /// Gets or sets the default template for the content control
    /// </summary>
    public object DefaultContentTemplate { get; set; }

    /// <summary>
    /// Getter for the attached property <see cref="ContentTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target user control</param>
    /// <returns>Content object</returns>
    public static object GetContentTemplate(UserControl control) => control.GetValue(ContentTemplateProperty);

    /// <summary>
    /// Setter for the attached property <see cref="ContentTemplateProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="template">Content object</param>
    public static void SetContentTemplate(UserControl control, object template) => control.SetValue(ContentTemplateProperty, template);

    // List of all DependencyProperty
    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.RegisterAttached("ContentTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateContentTemplate(e.NewValue)));
    public static readonly DependencyProperty CancelTemplateProperty = DependencyProperty.RegisterAttached("CancelTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateCancelTemplate(e.NewValue)));
    public static readonly DependencyProperty CancelVisibilityProperty = DependencyProperty.RegisterAttached("CancelVisibility", typeof(Visibility), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(Visibility.Visible, (_, e) => UpdateCancelVisibility(e.NewValue)));
    public static readonly DependencyProperty PreviousTemplateProperty = DependencyProperty.RegisterAttached("PreviousTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdatePreviousTemplate(e.NewValue)));
    public static readonly DependencyProperty PreviousVisibilityProperty = DependencyProperty.RegisterAttached("PreviousVisibility", typeof(Visibility), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(Visibility.Visible, (_, e) => UpdatePreviousVisibility(e.NewValue)));
    public static readonly DependencyProperty NextTemplateProperty = DependencyProperty.RegisterAttached("NextTemplate", typeof(object), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(null, (_, e) => UpdateNextTemplate(e.NewValue)));
    public static readonly DependencyProperty NextVisibilityProperty = DependencyProperty.RegisterAttached("NextVisibilityProperty", typeof(Visibility), typeof(SetupFlowNavigationBehavior), new PropertyMetadata(Visibility.Visible, (_, e) => UpdateNextVisibility(e.NewValue)));

    protected override void OnAttached()
    {
        base.OnAttached();

        // Set instance first
        _instance = this;

        Application.Current.GetService<SetupFlowOrchestrator>().PageChanging += OnPageChanging;
        ResetToDefault();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        var orchestrator = Application.Current.GetService<SetupFlowOrchestrator>();
        orchestrator.PageChanging -= OnPageChanging;

        _instance = null;
    }

    private void OnPageChanging(object sender, EventArgs args) => ResetToDefault();

    /// <summary>
    /// Sets the template of the associated <see cref="SetupFlowNavigation.ContentTemplate"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="templateObject">Template value</param>
    private static void UpdateContentTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.ContentTemplate = templateObject;
        }
    }

    /// <summary>
    /// Sets the template of the associated <see cref="SetupFlowNavigation.PreviousTemplate"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="templateObject">Template value</param>
    private static void UpdatePreviousTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.PreviousTemplate = templateObject;
        }
    }

    /// <summary>
    /// Sets the template of the associated <see cref="SetupFlowNavigation.CancelTemplate"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="templateObject">Template value</param>
    private static void UpdateCancelTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.CancelTemplate = templateObject;
        }
    }

    /// <summary>
    /// Sets the visibility of the associated <see cref="SetupFlowNavigation.CancelVisibility"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="visibilityObject">Template value</param>
    private static void UpdateCancelVisibility(object visibilityObject)
    {
        if (_instance != null && visibilityObject is Visibility visibility)
        {
            _instance.AssociatedObject.CancelVisibility = visibility;
        }
    }

    /// <summary>
    /// Sets the visibility of the associated <see cref="SetupFlowNavigation.PreviousVisibility"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="visibilityObject">Template value</param>
    private static void UpdatePreviousVisibility(object visibilityObject)
    {
        if (_instance != null && visibilityObject is Visibility visibility)
        {
            _instance.AssociatedObject.PreviousVisibility = visibility;
        }
    }

    /// <summary>
    /// Sets the template of the associated <see cref="SetupFlowNavigation.NextTemplate"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="templateObject">Template value</param>
    private static void UpdateNextTemplate(object templateObject)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.NextTemplate = templateObject;
        }
    }

    /// <summary>
    /// Sets the visibility of the associated <see cref="SetupFlowNavigation.NextVisibility"/>
    /// in the setup flow navigation
    /// </summary>
    /// <param name="visibilityObject">Template value</param>
    private static void UpdateNextVisibility(object visibilityObject)
    {
        if (_instance != null && visibilityObject is Visibility visibility)
        {
            _instance.AssociatedObject.NextVisibility = visibility;
        }
    }

    /// <summary>
    /// Resets all the navigation component templates to their default values
    /// </summary>
    private void ResetToDefault()
    {
        // Initialize to default template and values
        if (_instance != null)
        {
            UpdateContentTemplate(_instance.DefaultContentTemplate);
            UpdateCancelTemplate(_instance.DefaultCancelTemplate);
            UpdateCancelVisibility(Visibility.Visible);
            UpdatePreviousTemplate(_instance.DefaultPreviousTemplate);
            UpdatePreviousVisibility(Visibility.Visible);
            UpdateNextTemplate(_instance.DefaultNextTemplate);
            UpdateNextVisibility(Visibility.Visible);
        }
    }
}
