// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

/// <summary>
/// This UserControl is used to auto focus on the first selectable element.
/// Please inherit from this class for UserControl.
/// If the UserControl needs custom focus logic (for example, waiting until adaptive cards are loaded)
/// the individual UserControl should handle that.  Take a look at EnvironmentCreationOptionsView.xaml
/// for an example on using the autofocus behavior to focus when the element when it becomes visible.
/// </summary>
public class DevHomeUserControl : UserControl
{
    public DevHomeUserControl()
    {
        Loaded += (s, args) =>
        {
            Focus(FocusState.Programmatic);
        };
    }
}
