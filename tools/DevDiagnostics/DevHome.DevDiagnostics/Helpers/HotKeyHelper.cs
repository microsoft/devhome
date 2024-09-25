// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using WinUIEx.Messaging;

namespace DevHome.DevDiagnostics.Helpers;

// Note: instead of making this class disposable, we're disposing the WindowMessageMonitor in
// UnregisterHotKey, and MainWindow calls this in its Closing event handler.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public class HotKeyHelper// : IDisposable
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    internal ushort HotkeyID { get; private set; }

    private const string NoWindowHandleException = "Cannot get window handle: are you doing this too early?";
    private readonly HWND _windowHandle;
    private readonly Action<int> _onHotKeyPressed;
    private readonly WindowMessageMonitor _windowMessageMonitor;

    public HotKeyHelper(Window handlerWindow, Action<int> hotKeyHandler)
    {
        _onHotKeyPressed = hotKeyHandler;

        // Create a unique Id for this class in this instance.
        var atomName = $"{Environment.CurrentManagedThreadId:X8}{GetType().FullName}";
        HotkeyID = PInvoke.GlobalAddAtom(atomName);

        // Set up the window message hook to listen for hot keys.
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
        if (e.Message.MessageId == PInvoke.WM_HOTKEY)
        {
            var keyId = (int)e.Message.WParam;
            if (keyId == HotkeyID)
            {
                _onHotKeyPressed?.Invoke((int)e.Message.LParam);
                e.Handled = true;
            }
        }
    }

    internal void RegisterHotKey(VirtualKey key, HOT_KEY_MODIFIERS modifiers)
    {
        PInvoke.RegisterHotKey(_windowHandle, HotkeyID, modifiers, (uint)key);
    }

    internal void UnregisterHotKey()
    {
        if (HotkeyID != 0)
        {
            _ = PInvoke.UnregisterHotKey(_windowHandle, HotkeyID);
            PInvoke.GlobalDeleteAtom(HotkeyID);
            _windowMessageMonitor.Dispose();
            HotkeyID = 0;
        }
    }
}
