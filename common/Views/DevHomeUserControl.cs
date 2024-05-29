// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

public class DevHomeUserControl : UserControl
{
    public delegate void CanSetFocus();

    public DevHomeUserControl()
    {
        Loaded += (s, args) =>
        {
            Focus(FocusState.Programmatic);
        };
    }

    public void SetFocus()
    {
        Focus(FocusState.Programmatic);
    }
}
