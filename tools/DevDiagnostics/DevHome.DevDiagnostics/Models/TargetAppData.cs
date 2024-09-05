// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Principal;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using static DevHome.DevDiagnostics.Helpers.WindowHelper;

namespace DevHome.DevDiagnostics.Models;

public partial class TargetAppData : ObservableObject
{
    public static readonly TargetAppData Instance = new();

    public int ProcessId => TargetProcess?.Id ?? 0;

    public bool IsRunningAsSystem => TargetProcess?.SessionId == 0;

    public string Title { get; private set; } = string.Empty;

    public bool IsRunningAsAdmin
    {
        get
        {
            try
            {
                SafeFileHandle processToken;
                var result = PInvoke.OpenProcessToken(TargetProcess?.SafeHandle, Windows.Win32.Security.TOKEN_ACCESS_MASK.TOKEN_QUERY, out processToken);
                if (result != 0)
                {
                    var identity = new WindowsIdentity(processToken.DangerousGetHandle());
                    return identity?.Owner?.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid) ?? false;
                }

                return false;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == (int)WIN32_ERROR.ERROR_ACCESS_DENIED)
                {
                    return true;
                }

                return false;
            }
        }
    }

    [ObservableProperty]
    private SoftwareBitmapSource? _icon;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProcessId))]
    private Process? _targetProcess;

    internal HWND HWnd { get; private set; }

    [ObservableProperty]
    private bool _hasExited;

    private async void GetBitmap(Process process, HWND hWnd)
    {
        try
        {
            Bitmap? bitmap = null;

            if (hWnd != HWND.Null)
            {
                // First check if we can get an icon from the HWND
                bitmap = GetAppIcon(hWnd);
            }

            if (bitmap is null && process.MainWindowHandle != HWND.Null)
            {
                // If not, try and grab an icon from the process's main window
                bitmap = GetAppIcon((HWND)process.MainWindowHandle);
            }

            if (bitmap is null && process.MainModule is not null)
            {
                // Failing that, try and get the icon from the exe
                bitmap = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName)?.ToBitmap();
            }

            // Failing that, grab the default app icon
            bitmap ??= System.Drawing.Icon.FromHandle(LoadDefaultAppIcon()).ToBitmap();

            if (bitmap is not null)
            {
                Icon = await GetWinUI3BitmapSourceFromGdiBitmap(bitmap);
            }
            else
            {
                Icon = null;
            }
        }
        catch
        {
            Icon = null;
        }

        return;
    }

    private bool IsAppHost(string appName)
    {
        return string.Equals(appName, "ApplicationFrameHost", StringComparison.OrdinalIgnoreCase);
    }

    internal void SetNewAppData(Process process, HWND hWnd)
    {
        TargetProcess = process;
        HWnd = hWnd;

        // Reset hasExited, but don't trigger the property change event.
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        _hasExited = false;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        try
        {
            // These can throw if we don't have permissions to monitor process state.
            TargetProcess.EnableRaisingEvents = true;
            TargetProcess.Exited += TargetProcess_Exited;
        }
        catch
        {
        }

        Title = GetWindowTitle(hWnd) ?? TargetProcess.MainWindowTitle;

        // Getting the icon will be async
        GetBitmap(process, hWnd);

        AppName = IsAppHost(TargetProcess.ProcessName) ? Title : TargetProcess.ProcessName;

        OnPropertyChanged(nameof(AppName));
        OnPropertyChanged(nameof(TargetProcess));
        OnPropertyChanged(nameof(HWnd));
    }

    internal void ClearAppData()
    {
        Title = string.Empty;
        AppName = string.Empty;
        Icon = null;
        TargetProcess?.Dispose();
        TargetProcess = null;

        OnPropertyChanged(nameof(AppName));
        OnPropertyChanged(nameof(TargetProcess));
        OnPropertyChanged(nameof(HWnd));
        OnPropertyChanged(nameof(Icon));
    }

    private void TargetProcess_Exited(object? sender, EventArgs e)
    {
        // Change the property, so that we trigger the property change event.
        HasExited = true;
    }
}
