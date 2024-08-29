// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Helpers;
using DevHome.DevInsights.Models;
using DevHome.DevInsights.Properties;
using DevHome.DevInsights.Views;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Windows.System;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;
using static DevHome.DevInsights.Helpers.WindowHelper;

namespace DevHome.DevInsights;

public sealed partial class PrimaryWindow : WindowEx
{
    private const VirtualKey HotKey = VirtualKey.F12;

    private const HOT_KEY_MODIFIERS KeyModifier = HOT_KEY_MODIFIERS.MOD_WIN;

    private readonly WERHelper _werHelper;

    private HotKeyHelper? _hotKeyHelper;

    private ServicingHelper? _servicingHelper;

    public BarWindow? DBarWindow { get; private set; }

    public PrimaryWindow()
    {
        InitializeComponent();
        _werHelper = Application.Current.GetService<WERHelper>();
        _werHelper.Start();
    }

    public void ShowBarWindow()
    {
        if (DBarWindow == null)
        {
            DBarWindow = new();
        }
        else
        {
            PInvoke.ShowWindow(DBarWindow.ThisHwnd, SHOW_WINDOW_CMD.SW_RESTORE);

            // Activate is unreliable so use SetForegroundWindow
            PInvoke.SetForegroundWindow(DBarWindow.ThisHwnd);
        }
    }

    public void ClearBarWindow()
    {
        DBarWindow = null;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        App.Log("DevHome_DevInsights_MainWindows_Loaded", LogLevel.Measure);
        _servicingHelper = new(this, HandleSessionEnd);
        _hotKeyHelper = new(this, HandleHotKey);
        _hotKeyHelper.RegisterHotKey(HotKey, KeyModifier);
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        DBarWindow?.Close();
        _hotKeyHelper?.UnregisterHotKey();
        _servicingHelper?.Unregister();
    }

    public void HandleSessionEnd()
    {
        App.Log("DevHome_DevInsights_SessionEnd", LogLevel.Info);
        DBarWindow?.Close();
        Process.GetCurrentProcess().Kill();
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

            TranslateUWPProcess(hWnd, ref process);
            TargetAppData.Instance.SetNewAppData(process, hWnd);
        }

        ShowBarWindow();
    }
}
