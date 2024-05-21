// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Specialized;
using DevHome.Common.Extensions;
using DevHome.PI.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.PI.ToolWindows;

public class ToolWindow : WindowEx
{
    // To allow this base window to use derived-window-specific settings,
    // we need to pass the settings by ref, hence the use of a StringCollection.
    private readonly double defaultTop = 50;
    private readonly double defaultLeft = 100;
    private readonly double defaultWidth = 400;
    private readonly double defaultHeight = 400;
    private readonly StringCollection? position;

    public ToolWindow()
    {
        position = null;
        AppWindow.Closing += AppWindow_Closing;

        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.OpenChildWindows.Add(this);
    }

    public ToolWindow(StringCollection pos)
        : this()
    {
        position = pos;
        if (position != null)
        {
            var top = double.TryParse(position[0], out var y) ? y : defaultTop;
            var left = double.TryParse(position[1], out var x) ? x : defaultLeft;
            var width = double.TryParse(position[2], out var cx) ? cx : defaultWidth;
            var height = double.TryParse(position[3], out var cy) ? cy : defaultHeight;
            this.MoveAndResize(left, top, width, height);
        }
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.OpenChildWindows.Remove(this);

        if (position != null)
        {
            position[0] = $"{AppWindow.Position.Y}";
            position[1] = $"{AppWindow.Position.X}";
            position[2] = $"{AppWindow.Size.Width}";
            position[3] = $"{AppWindow.Size.Height}";
            Settings.Default.Save();
        }
    }
}
