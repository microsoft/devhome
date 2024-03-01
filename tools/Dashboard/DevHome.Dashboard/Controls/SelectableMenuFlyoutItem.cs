// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard.Controls;

public sealed class SelectableMenuFlyoutItem : MenuFlyoutItem, ISelectionItemProvider
{
    private bool _selected;

    public bool IsSelected
    {
        get => _selected;
        set => _selected = value;
    }

    public IRawElementProviderSimple SelectionContainer => throw new NotImplementedException();

    public void AddToSelection() => throw new NotImplementedException();

    public void RemoveFromSelection()
    {
        IsSelected = false;
    }

    public void Select()
    {
        IsSelected = true;
    }
}
