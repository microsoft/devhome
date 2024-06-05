// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Win32;
using Windows.Win32.UI.Controls.Dialogs;

namespace DevHome.PI.Controls;

public sealed partial class AddToolControl : UserControl
{
    private readonly string invalidToolInfo = CommonHelper.GetLocalizedString("InvalidToolInfoMessage");
    private readonly string messageCloseText = CommonHelper.GetLocalizedString("MessageCloseText");

    public AddToolControl()
    {
        InitializeComponent();
    }

    private void ToolBrowseButton_Click(object sender, RoutedEventArgs e)
    {
        HandleBrowseButton();
    }

    private void HandleBrowseButton()
    {
        // WinUI3's OpenFileDialog does not work if we're running elevated. So we have to use the old Win32 API.
        var fileName = string.Empty;
        var filter = "Executables (*.exe)\0*.exe\0Batch Files (*.bat)\0*.bat\0\0";
        var filterarray = filter.ToCharArray();
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;

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

                    // TODO - This should be the Settings window, not the bar window
                    hwndOwner = barWindow?.CurrentHwnd ?? Windows.Win32.Foundation.HWND.Null,
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
        var tool = GetCurrentToolDefinition();
        if (tool is null)
        {
            return;
        }

        ExternalToolsHelper.Instance.AddExternalTool(tool);
        var toolRegisteredMessage = CommonHelper.GetLocalizedString("ToolRegisteredMessage", ToolNameTextBox.Text);
        WindowHelper.ShowTimedMessageDialog(this, toolRegisteredMessage, messageCloseText);
        ClearValues();
    }

    private void ClearValues()
    {
        ToolNameTextBox.Text = string.Empty;
        ToolPathTextBox.Text = string.Empty;
        NoneRadio.IsChecked = true;
        PrefixTextBox.Text = string.Empty;
        OtherArgsTextBox.Text = string.Empty;
        IsPinnedToggleSwitch.IsOn = true;
    }

    private ExternalTool? GetCurrentToolDefinition()
    {
        if (string.IsNullOrEmpty(ToolNameTextBox.Text) || string.IsNullOrEmpty(ToolPathTextBox.Text))
        {
            WindowHelper.ShowTimedMessageDialog(this, invalidToolInfo, messageCloseText);
            return null;
        }

        var argType = ExternalToolArgType.None;

        if (HwndRadio.IsChecked ?? false)
        {
            argType = ExternalToolArgType.Hwnd;
        }
        else if (ProcessIdRadio.IsChecked ?? false)
        {
            argType = ExternalToolArgType.ProcessId;
        }

        return new(
            ToolNameTextBox.Text,
            ToolPathTextBox.Text,
            argType,
            PrefixTextBox.Text ?? string.Empty,
            OtherArgsTextBox.Text ?? string.Empty,
            IsPinnedToggleSwitch.IsOn);
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
        if (SampleCommandTextBox is null)
        {
            // The window is still initializing.
            return;
        }

        var tool = GetCurrentToolDefinition();
        if (tool is null)
        {
            return;
        }

        SampleCommandTextBox.Text = tool.CreateFullCommandLine(123, (Windows.Win32.Foundation.HWND)123);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        ClearValues();
    }
}
