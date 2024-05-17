// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Models;

public class AutoFocusPage : Page
{
    public AutoFocusPage()
    {
        Loaded += (s, e) => { this.Focus(FocusState.Programmatic); };
    }
}
