// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.Properties;
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
    private static readonly string _clipboardMonitorToolName = CommonHelper.GetLocalizedString("ClipboardMonitorName");

    private ClipboardMonitoringWindow? _clipboardMonitoringWindow;

    public ClipboardMonitorInternalTool()
        : base(_clipboardMonitorToolName, ToolType.Unknown, Settings.Default.IsClipboardMonitorToolPinned)
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

    internal override void InvokeTool(Window? parent, int? targetProcessId, HWND hWnd, string? commandLineParams)
    {
        if (_clipboardMonitoringWindow is null || _clipboardMonitoringWindow.AppWindow is null)
        {
            _clipboardMonitoringWindow = new ClipboardMonitoringWindow();
        }

        if (parent is not null)
        {
            RectInt32 rect;
            rect.X = parent.AppWindow.Position.X;
            rect.Y = parent.AppWindow.Position.Y + 100;
            rect.Width = parent.AppWindow.Size.Width;
            rect.Height = _clipboardMonitoringWindow.AppWindow.Size.Height;

            _clipboardMonitoringWindow.AppWindow.MoveAndResize(rect);
        }

        _clipboardMonitoringWindow.Activate();
    }

    protected override void OnIsPinnedChange(bool newValue)
    {
        Settings.Default.IsClipboardMonitorToolPinned = newValue;
        Settings.Default.Save();
    }

    public override void UnregisterTool()
    {
        // Ignore this command for now until we have a way for the user to discover unregistered internal tools
    }
}
