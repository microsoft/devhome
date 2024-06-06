// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using WinUIEx;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI;

public sealed partial class PrimaryWindow : WindowEx
{
    private const VirtualKey HotKey = VirtualKey.F12;

    private const HOT_KEY_MODIFIERS KeyModifier = HOT_KEY_MODIFIERS.MOD_WIN;
    private HotKeyHelper? hotKeyHelper;

    public BarWindow? DBarWindow { get; private set; }

    public PrimaryWindow()
    {
        InitializeComponent();
        ExternalToolsHelper.Instance.Init();
    }

    public void ShowBarWindow()
    {
        if (DBarWindow == null)
        {
            DBarWindow = new();
        }
        else
        {
            // Activate is unreliable so use SetForegroundWindow
            PInvoke.SetForegroundWindow(DBarWindow.CurrentHwnd);
        }
    }

    public void ClearBarWindow()
    {
        DBarWindow = null;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        hotKeyHelper = new(this, HandleHotKey);
        hotKeyHelper.RegisterHotKey(HotKey, KeyModifier);

        App.Log("DevHome.PI_MainWindows_Loaded", LogLevel.Measure);
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        DBarWindow?.Close();
        hotKeyHelper?.UnregisterHotKey();
    }

    public void HandleHotKey(int keyId)
    {
        var hWnd = FindVisibleForegroundWindow(Settings.Default.ExcludedProcesses);

        if (hWnd != IntPtr.Zero)
        {
            Process? process = null;

            try
            {
                var processId = GetProcessIdFromWindow(hWnd);
                if (processId != 0)
                {
                    process = Process.GetProcessById((int)processId);
                }
            }
            catch
            {
            }

            if (process == null)
            {
                // Process must have died before we had a chance to grab it's process object.
                return;
            }

            TargetAppData.Instance.SetNewAppData(process, hWnd);
        }

        DBarWindow ??= new();
    }
}
