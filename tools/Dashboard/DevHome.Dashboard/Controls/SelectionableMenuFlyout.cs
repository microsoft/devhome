// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Dashboard.Controls;

public sealed class SelectionableMenuFlyout : MenuFlyout, ISelectionProvider
{
    public bool CanSelectMultiple => false;

    public bool IsSelectionRequired => false;

    public IRawElementProviderSimple[] GetSelection()
    {
        var res = Array.Empty<IRawElementProviderSimple>();

        var num_children = VisualTreeHelper.GetChildrenCount(this);
        for (var i = 0; i < num_children; ++i)
        {
            var child = VisualTreeHelper.GetChild(this, i) as ISelectionItemProvider;

            if (child.IsSelected)
            {
                res = (IRawElementProviderSimple[])res.Append(VisualTreeHelper.GetChild(this, i) as IRawElementProviderSimple);
                return res;
            }
        }

        return res;
    }
}
