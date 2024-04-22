// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard.Controls;

public sealed class SelectableMenuFlyoutItem : MenuFlyoutItem
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SelectableMenuFlyoutItemAutomationPeer(this);
    }
}

public class SelectableMenuFlyoutItemAutomationPeer : MenuFlyoutItemAutomationPeer, ISelectionItemProvider
{
    private readonly FrameworkElement _owner;
    private bool _selected;

    public bool IsSelected
    {
        get => _selected;
        set => _selected = value;
    }

    public SelectableMenuFlyoutItemAutomationPeer(SelectableMenuFlyoutItem owner)
        : base(owner)
    {
        _owner = owner;
    }

    public IRawElementProviderSimple SelectionContainer => (IRawElementProviderSimple)_owner.Parent;

    public void AddToSelection()
    {
        IsSelected = true;
    }

    public void RemoveFromSelection()
    {
        IsSelected = false;
    }

    public void Select()
    {
        Invoke();
    }

    protected override string GetClassNameCore()
    {
        return "SelectableMenuItemFlyoutItem";
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.SelectionItem)
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }
}
