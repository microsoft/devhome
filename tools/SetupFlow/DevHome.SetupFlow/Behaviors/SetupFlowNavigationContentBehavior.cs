// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.SetupFlow.Behaviors;

/// <summary>
/// Behavior class for customizing the setup flow navigation content
/// </summary>
public class SetupFlowNavigationContentBehavior : Behavior<ContentControl>
{
    /// <summary>
    /// Singleton instance of this behavior class implemented by the setup flow
    /// root page content control.
    /// </summary>
    private static SetupFlowNavigationContentBehavior _instance;

    protected override void OnAttached()
    {
        base.OnAttached();
        _instance = this;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _instance = null;
    }

    /// <summary>
    /// Sets the content of the associated <see cref="ContentControl"/> in the setup flow navigation
    /// </summary>
    /// <param name="content">Customized content</param>
    public static void SetNavigationContent(object content)
    {
        if (_instance != null)
        {
            _instance.AssociatedObject.Content = content;
        }
    }

    /// <summary>
    /// Getter for the attached property <see cref="ContentProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <returns>Content object</returns>
    public static object GetContent(UserControl control) => control.GetValue(ContentProperty);

    /// <summary>
    /// Setter for the attached property <see cref="ContentProperty"/>
    /// </summary>
    /// <param name="control">Target control</param>
    /// <param name="content">Content object</param>
    public static void SetContent(UserControl control, object content)
    {
        control.SetValue(ContentProperty, content);

        // Remove navigation content before navigating to a new page
        control.Unloaded += (_, _) => SetNavigationContent(null);
    }

    public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached("Content", typeof(ControlTemplate), typeof(SetupFlowNavigationContentBehavior), new PropertyMetadata(null, (_, e) => SetNavigationContent(e.NewValue)));
}
