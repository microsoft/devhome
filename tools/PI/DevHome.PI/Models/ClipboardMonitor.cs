// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace DevHome.PI.Models;

internal sealed class ClipboardMonitor : WindowHooker<ClipboardMonitor>, INotifyPropertyChanged
{
    public static readonly ClipboardMonitor Instance = new();

    public ClipboardContents Contents { get; private set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    internal ClipboardMonitor()
    {
    }

    private void ClipboardChanged()
    {
        SafeHandle? h = null;
        ClipboardContents newContents = new();
        try
        {
            var clipboardText = string.Empty;
            PInvoke.OpenClipboard(ListenerHwnd);
            h = PInvoke.GetClipboardData_SafeHandle(13 /* CF_UNICODETEXT */);
            if (!h.IsInvalid)
            {
                unsafe
                {
                    var p = PInvoke.GlobalLock(h);
                    clipboardText = Marshal.PtrToStringUni((IntPtr)p) ?? string.Empty;
                }

                if (clipboardText != string.Empty)
                {
                    newContents = ParseClipboardContents(clipboardText);
                }
            }
        }
        finally
        {
            if (h is not null && !h.IsInvalid)
            {
                PInvoke.GlobalUnlock(h);

                // You're not suppose to close this handle.
                h.SetHandleAsInvalid();
            }

            PInvoke.CloseClipboard();

            Contents = newContents;
            OnPropertyChanged(nameof(Contents));
        }
    }

    /* TODO This pattern matches the following:
        100
        0x100
        0x80040005
        -2147221499
        -1
        0xabc
        abc
        ffffffff
        1de
        cab
        bee

       ...but sequences like "cab", "bee", "fed" could be false positives. We need
       more logic to exclude these.
    */
    private static readonly Regex _findNumbersRegex =
        new(
            pattern: @"(?:0[xX][0-9A-Fa-f]+|-?\b(?:\d+|\d*\.\d+)\b|\b[0-9A-Fa-f]+\b)",
            options: RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private ClipboardContents ParseClipboardContents(string text)
    {
        ClipboardContents newContents = new();

        // If this text contains a number, show it in different number bases.
        var matches = _findNumbersRegex.Matches(text);
        var converter = new Int32Converter();

        foreach (var match in matches.Cast<Match>())
        {
            var original = match.ToString();

            // Assume the number is easily identifable as either base 10 or base 16... convert to int.
            int? errorAsInt;

            try
            {
                if (converter.IsValid(original))
                {
                    // Int32Converter.ConvertFromString() does a pretty good job of parsing numbers, except when given a hex
                    // number that isn't prefixed with 0x. If it fails, try parsing it using int.Parse().
                    errorAsInt = (int?)converter.ConvertFromString(original);
                }
                else
                {
                    errorAsInt = int.Parse(original, NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                }
            }
            catch
            {
                // If this ConvertFromString() function fails due to a bad format, update the above regex to ensure
                // the bad string isn't fed to this function.
                Log.Warning("Failed to parse \" {original} \" to a number", original);
                return newContents;
            }

            newContents.Raw = original;
            newContents.Hex = errorAsInt is not null ? Convert.ToString((int)errorAsInt, 16) : original;
            newContents.Dec = errorAsInt is not null ? Convert.ToString((int)errorAsInt, 10) : original;

            // Is there an error code on here?
            // if (ErrorLookupHelper.ContainsErrorCode(text, out var hresult))
            if (errorAsInt is not null)
            {
                var errors = ErrorLookupHelper.LookupError((int)errorAsInt);
                if (errors is not null)
                {
                    foreach (var error in errors)
                    {
                        // Seperate each error with a space. These errors aren't localized, so we may not need to worry
                        // about the space being in the wrong place.
                        if (newContents.Code != string.Empty)
                        {
                            newContents.Code += " ";
                            newContents.Help += " ";
                        }

                        newContents.Code += error.Name;
                        newContents.Help += error.Help;
                    }
                }
            }

            break;
        }

        return newContents;
    }

    public void Start()
    {
        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        Start((HWND)primaryWindow.GetWindowHandle());
    }

    public override void Start(HWND hwndUsedForListening)
    {
        base.Start(hwndUsedForListening);

        var success = PInvoke.AddClipboardFormatListener(ListenerHwnd);
        if (!success)
        {
            Log.Error("AddClipboardFormatListener failed: {GetLastError}", Marshal.GetLastWin32Error().ToString(CultureInfo.CurrentCulture));
        }
    }

    public override void Stop()
    {
        if (ListenerHwnd != HWND.Null)
        {
            PInvoke.RemoveClipboardFormatListener(ListenerHwnd);

            base.Stop();
        }
    }

    protected override LRESULT CustomWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_CLIPBOARDUPDATE:
                {
                    ThreadPool.QueueUserWorkItem((o) => ClipboardChanged());
                    break;
                }
        }

        return base.CustomWndProc(hWnd, msg, wParam, lParam);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
