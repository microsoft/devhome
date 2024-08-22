// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.DevInsights.Helpers;
using DevHome.DevInsights.Models;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.DevInsights.Controls;

public class ProcessSelectionButton : Button
{
    public ProcessSelectionButton()
    {
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);

        // When the mouse cursor is over the button, change to the default cursor.
        ResetCursor();
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        // When the mouse cursor leaves the button, change the cursor to the cross.
        ChangeCursor();
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Were we showing the select cursor?
        if (ProtectedCursor == null)
        {
            return;
        }

        Process? p;
        Windows.Win32.Foundation.HWND hwnd;

        // Grab the window under the cursor and attach to that process.
        WindowHelper.GetAppInfoUnderMouseCursor(out p, out hwnd);
        if (p != null)
        {
            WindowHelper.TranslateUWPProcess(hwnd, ref p);
            TargetAppData.Instance.SetNewAppData(p, hwnd);
        }

        ResetCursor();
    }

    public void ChangeCursor()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Cross);
    }

    public void ResetCursor()
    {
        ProtectedCursor = null;
    }
}
