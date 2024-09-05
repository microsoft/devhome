// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Properties;
using DevHome.DevDiagnostics.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace DevHome.DevDiagnostics.Helpers;

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

    internal override void InvokeTool(ToolLaunchOptions options)
    {
        if (_clipboardMonitoringWindow is null || _clipboardMonitoringWindow.AppWindow is null)
        {
            _clipboardMonitoringWindow = new ClipboardMonitoringWindow();

            // Add this window to the list of related windows for the BarWindow,
            // so that we can ensure that any BarWindow theme changes are also propagated to this window.
            var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
            barWindow?.AddRelatedWindow(_clipboardMonitoringWindow);
        }

        if (options.ParentWindow is not null)
        {
            RectInt32 rect;
            rect.X = options.ParentWindow.AppWindow.Position.X;
            rect.Y = options.ParentWindow.AppWindow.Position.Y + 100;
            rect.Width = options.ParentWindow.AppWindow.Size.Width;
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
