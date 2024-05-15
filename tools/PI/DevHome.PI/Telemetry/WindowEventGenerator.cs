// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading;
using DevHome.Common.Extensions;
using DevHome.PI;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace DevHome.Telemetry;

internal sealed class WindowEventGenerator : WindowHooker<WindowEventGenerator>, IDisposable
{
    private readonly Window window;
    private readonly Mutex visibilityMutex = new();
    private readonly Mutex focusMutex = new();
    private bool currentlyVisible;
    private bool currentlyFocused;

    internal bool CurrentlyVisible => currentlyVisible;

    internal bool CurrentlyFocused => currentlyFocused;

    internal enum InteractiveUsageEventType
    {
        Start,
        Stop,
    }

    internal sealed class InteractiveUsageEventArgs : EventArgs
    {
        internal InteractiveUsageEventArgs(InteractiveUsageEventType usageType)
        {
            UsageType = usageType;
        }

        public InteractiveUsageEventType UsageType { get; private set; }
    }

    public event System.EventHandler<InteractiveUsageEventArgs>? InteractiveUsageVisibilityEvent;

    public event System.EventHandler<InteractiveUsageEventArgs>? InteractiveUsageFocusEvent;

    public WindowEventGenerator(Window window)
    {
        this.window = window;
        Start(new HWND(window.GetWindowHandle()));
    }

    private void FireInteractiveUsageVisibilityEvent(bool newValue)
    {
        InteractiveUsageEventArgs interactiveArgs;
        var oldValue = false;
        var triggerEvent = false;
        {
            if (visibilityMutex.WaitOne())
            {
                oldValue = currentlyVisible;

                if (newValue != oldValue)
                {
                    triggerEvent = true;
                    currentlyVisible = newValue;
                }
            }
        }

        if (triggerEvent)
        {
            if (newValue)
            {
                interactiveArgs = new InteractiveUsageEventArgs(InteractiveUsageEventType.Start);
            }
            else
            {
                interactiveArgs = new InteractiveUsageEventArgs(InteractiveUsageEventType.Stop);
            }

            var raiseEvent = InteractiveUsageVisibilityEvent;
            if (raiseEvent != null)
            {
                raiseEvent(this, interactiveArgs);
            }
        }
    }

    private void FireInteractiveUsageFocusEvent(bool newValue)
    {
        InteractiveUsageEventArgs interactiveArgs;
        var oldValue = false;
        var triggerEvent = false;
        {
            if (focusMutex.WaitOne())
            {
                oldValue = currentlyFocused;

                if (newValue != oldValue)
                {
                    triggerEvent = true;
                    currentlyFocused = newValue;
                }
            }
        }

        if (triggerEvent)
        {
            if (newValue)
            {
                interactiveArgs = new InteractiveUsageEventArgs(InteractiveUsageEventType.Start);
            }
            else
            {
                interactiveArgs = new InteractiveUsageEventArgs(InteractiveUsageEventType.Stop);
            }

            var raiseEvent = InteractiveUsageFocusEvent;
            if (raiseEvent != null)
            {
                raiseEvent(this, interactiveArgs);
            }
        }
    }

    protected override LRESULT CustomWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_ACTIVATE:
                {
                    if (wParam == 0)
                    {
                        FireInteractiveUsageVisibilityEvent(false);
                    }
                    else
                    {
                        FireInteractiveUsageVisibilityEvent(true);
                    }

                    break;
                }

            case PInvoke.WM_SETFOCUS:
                {
                    FireInteractiveUsageFocusEvent(true);
                    break;
                }

            case PInvoke.WM_KILLFOCUS:
                {
                    FireInteractiveUsageFocusEvent(false);
                    break;
                }
        }

        return base.CustomWndProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        visibilityMutex.Dispose();
        focusMutex.Dispose();
    }
}
