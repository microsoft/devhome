// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.PI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.Win32.Foundation;

namespace DevHome.PI.Helpers;

public class ClipboardMonitorInternalTool : Tool
{
    private const string ClipboardButtonText = "\uf0e3"; // ClipboardList

    public ClipboardMonitorInternalTool()
        : base("Clipboard Monitor", true)
    {
    }

    public override IconElement GetIcon()
    {
        return new FontIcon
        {
            Glyph = ClipboardButtonText,
            FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
        };
    }

    internal override void InvokeTool(Window? parent, int? targetProcessId, HWND hWnd)
    {
        ClipboardMonitoringWindow clipboardMonitoringWindow = new();
        clipboardMonitoringWindow.Activate();

        if (parent is not null)
        {
            RectInt32 rect;
            rect.X = parent.AppWindow.Position.X;
            rect.Y = parent.AppWindow.Position.Y + 100;
            rect.Width = parent.AppWindow.Size.Width;
            rect.Height = clipboardMonitoringWindow.AppWindow.Size.Height;

            clipboardMonitoringWindow.AppWindow.MoveAndResize(rect);
        }
    }

    public override void UnregisterTool()
    {
        // Ignore this command for now
    }
}
