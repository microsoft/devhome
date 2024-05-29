// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

public class DevHomePage : Page
{
    public delegate void CanSetFocus();

    public DevHomePage()
    {
        Loaded += (s, e) =>
        {
            Focus(FocusState.Programmatic);
        };
    }

    public void SetFocus()
    {
        Focus(FocusState.Programmatic);
    }
}
