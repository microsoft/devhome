// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevDiagnostics.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.DevDiagnostics.Helpers;

public class ExpanderBehavior : Behavior<Expander>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.Expanding += AssociatedObject_Expanding;
        AssociatedObject.Collapsed += OnCollapsed;
    }

    private void AssociatedObject_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        if (AssociatedObject.DataContext is Insight insight && !insight.HasBeenRead)
        {
            insight.HasBeenRead = true;
            insight.BadgeOpacity = 0;
        }
    }

    private void OnCollapsed(Expander sender, ExpanderCollapsedEventArgs args)
    {
        // Do nothing: once we've set HasBeenRead to true, we don't change it again.
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.Expanding -= AssociatedObject_Expanding;
        AssociatedObject.Collapsed -= OnCollapsed;
    }
}
