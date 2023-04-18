// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace DevHome.Stub.Controls;

public class WinAutomationWindowAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
    private readonly WinAutomationWindow _owner;

    public WinAutomationWindowAutomationPeer(Window owner)
        : base(owner)
    {
        _owner = (WinAutomationWindow)owner;
    }

    public WindowInteractionState InteractionState
    {
        get
        {
            if (_owner.IsActive)
            {
                return WindowInteractionState.ReadyForUserInteraction;
            }

            if (_owner.IsLoaded)
            {
                return WindowInteractionState.Running;
            }

            return WindowInteractionState.Closing;
        }
    }

    public virtual bool IsModal => false;

    public bool IsTopmost => _owner.Topmost;

    public bool Maximizable => _owner.IsMaxButtonEnabled;

    public bool Minimizable => _owner.IsMinButtonEnabled;

    public WindowVisualState VisualState
    {
        get
        {
            switch (_owner.WindowState)
            {
                case WindowState.Normal: return WindowVisualState.Normal;
                case WindowState.Minimized: return WindowVisualState.Minimized;
                case WindowState.Maximized: return WindowVisualState.Maximized;
                default: return WindowVisualState.Normal;
            }
        }
    }

    public void Close()
    {
        _owner.Close();
    }

    public void SetVisualState(WindowVisualState state)
    {
        switch (state)
        {
            case WindowVisualState.Normal:
                _owner.WindowState = WindowState.Normal;
                break;
            case WindowVisualState.Maximized:
                _owner.WindowState = WindowState.Maximized;
                break;
            case WindowVisualState.Minimized:
                _owner.WindowState = WindowState.Minimized;
                break;
        }
    }

    public bool WaitForInputIdle(int milliseconds)
    {
        return true;
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Window;
    }

    protected override string GetClassNameCore()
    {
        return nameof(WinAutomationWindow);
    }

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();

        if (string.IsNullOrEmpty(name))
        {
            name = _owner.Title ?? string.Empty;
        }

        return name;
    }
}
