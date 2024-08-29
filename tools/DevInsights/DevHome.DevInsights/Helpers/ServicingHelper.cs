// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx.Messaging;

namespace DevHome.DevInsights.Helpers;

// Note: instead of making this class disposable, we're disposing the WindowMessageMonitor in
// UnregisterHotKey, and MainWindow calls this in its Closing event handler.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public class ServicingHelper
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private const string NoWindowHandleException = "Cannot get window handle: are you doing this too early?";
    private readonly HWND _windowHandle;
    private readonly WindowMessageMonitor _windowMessageMonitor;
    private readonly Action _onSessionEnd;

    public ServicingHelper(Window handlerWindow, Action handleSessionEnd)
    {
        _onSessionEnd = handleSessionEnd;

        // Set up the window message hook to listen for session end messages.
        _windowHandle = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(handlerWindow);
        if (_windowHandle.IsNull)
        {
            throw new InvalidOperationException(NoWindowHandleException);
        }

        _windowMessageMonitor = new WindowMessageMonitor(_windowHandle);
        _windowMessageMonitor.WindowMessageReceived += OnWindowMessageReceived;
    }

    private void OnWindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        if (e.Message.MessageId == PInvoke.WM_ENDSESSION)
        {
            _onSessionEnd?.Invoke();
            e.Handled = true;
        }
    }

    internal void Unregister()
    {
        _windowMessageMonitor?.Dispose();
    }
}
