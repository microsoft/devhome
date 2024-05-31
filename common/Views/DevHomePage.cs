// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

/// <summary>
/// This page is used to auto focus on the first selectable element.
/// Please inherit from this class for pages.
/// If the Page needs custom focus logic (for example, waiting until adaptive cards are loaded)
/// the individual Page should handle that.  Take a look at EnvironmentCreationOptionsView.xaml
/// for an example on using the autofocus behavior to focus when the element when it becomes visible.
/// </summary>
/// </summary>
public class DevHomePage : Page
{
    public DevHomePage()
    {
        Loaded += (s, e) =>
        {
            Focus(FocusState.Programmatic);
        };
    }
}
