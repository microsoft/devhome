// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

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
