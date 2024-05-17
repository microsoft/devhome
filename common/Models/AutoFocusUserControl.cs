// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Models;

public class AutoFocusUserControl : UserControl
{
    public AutoFocusUserControl()
    {
        Loaded += (s, args) => { this.Focus(FocusState.Programmatic); };
    }
}
