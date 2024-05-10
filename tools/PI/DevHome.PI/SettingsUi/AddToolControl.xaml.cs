// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;

namespace DevHome.PI.SettingsUi;

public sealed partial class AddToolControl : UserControl
{
    public AddToolControl()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        HandleBrowseButton();
    }

    private void HandleBrowseButton()
    {
        // Unfortunately WinUI3's OpenFileDialog does not work if we're running elevated. So we have to use the old Win32 API.
        var fileName = string.Empty;
        var filter = "Executables (*.exe)\0*.exe\0Batch Files (*.bat)\0*.bat\0\0";
        var filterarray = filter.ToCharArray();
        unsafe
        {
            fixed (char* file = new char[255], pFilter = filterarray)
            {
                var openfile = new OPENFILENAMEW
                {
                    lStructSize = (uint)Marshal.SizeOf<OPENFILENAMEW>(),
                    lpstrFile = new Windows.Win32.Foundation.PWSTR(file),
                    lpstrFilter = pFilter,
                    nFilterIndex = 1,
                    nMaxFile = 255,
                    hwndOwner = BarWindow.ThisHwnd,
                };

                if (PInvoke.GetOpenFileName(ref openfile))
                {
                    fileName = new string(openfile.lpstrFile);
                }
            }
        }

        if (fileName != string.Empty)
        {
            ToolPathTextBox.Text = fileName;
            if (ToolNameTextBox.Text == string.Empty)
            {
                ToolNameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(fileName);
            }
        }

        return;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        ExternalToolsHelper.Instance.AddExternalTool(GetCurrentToolDefinition());

        // Clear everything now
        ClearValues();
    }

    private void ClearValues()
    {
        ToolNameTextBox.Text = string.Empty;
        ToolPathTextBox.Text = string.Empty;
        NoneRadio.IsChecked = true;
        PrefixTextBox.Text = string.Empty;
        OtherArgsTextBox.Text = string.Empty;
    }

    private ExternalTool GetCurrentToolDefinition()
    {
        var argType = ExternalToolArgType.None;

        if (HwndRadio?.IsChecked ?? false)
        {
            argType = ExternalToolArgType.Hwnd;
        }
        else if (ProcessIdRadio?.IsChecked ?? false)
        {
            argType = ExternalToolArgType.ProcessId;
        }

        return new(ToolNameTextBox.Text, ToolPathTextBox.Text, argType, PrefixTextBox?.Text ?? string.Empty, OtherArgsTextBox?.Text ?? string.Empty);
    }

    private void UpdateSampleCommandline(object sender, TextChangedEventArgs e)
    {
        UpdateSampleCommandline();
    }

    private void UpdateSampleCommandline(object sender, RoutedEventArgs e)
    {
        UpdateSampleCommandline();
    }

    private void UpdateSampleCommandline()
    {
        if (SampleCommandTextBox == null)
        {
            // The window is still initializing.
            return;
        }

        var tool = GetCurrentToolDefinition();

        SampleCommandTextBox.Text = tool.CreateFullCommandLine(123, (Windows.Win32.Foundation.HWND)123);
    }
}
