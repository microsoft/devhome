// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard.Controls;

public sealed class SelectionableMenuFlyout : MenuFlyout, ISelectionProvider
{
    public bool CanSelectMultiple => false;

    public bool IsSelectionRequired => false;

    public IRawElementProviderSimple[] GetSelection() => throw new NotImplementedException();
}
