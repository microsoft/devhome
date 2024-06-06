﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace DevHome.PI.ViewModels;

public partial class BarWindowViewModel : ObservableObject
{
    private const string _UnsnapButtonText = "\ue89f";
    private const string _SnapButtonText = "\ue8a0";

    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string _errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly List<Button> _externalToolButtons = [];

    private readonly ObservableCollection<Button> _externalTools = [];
    private readonly SnapHelper _snapHelper;

    [ObservableProperty]
    private string _systemCpuUsage = string.Empty;

    [ObservableProperty]
    private string _systemRamUsage = string.Empty;

    [ObservableProperty]
    private string _systemDiskUsage = string.Empty;

    [ObservableProperty]
    private bool _isSnappingEnabled = false;

    [ObservableProperty]
    private string _currentSnapButtonText = _SnapButtonText;

    [ObservableProperty]
    private string _appCpuUsage = string.Empty;

    [ObservableProperty]
    private Visibility _appBarVisibility = Visibility.Visible;

    [ObservableProperty]
    private string _applicationName = string.Empty;

    [ObservableProperty]
    private int _applicationPid;

    [ObservableProperty]
    private SoftwareBitmapSource? _applicationIcon;

    [ObservableProperty]
    private Orientation _barOrientation = Orientation.Horizontal;

    [ObservableProperty]
    private bool _isSnapped;

    [ObservableProperty]
    private bool _showingExpandedContent;

    [ObservableProperty]
    private bool _isAlwaysOnTop = true;

    [ObservableProperty]
    private PointInt32 _windowPosition;

    internal HWND? ApplicationHwnd { get; private set; }

    public BarWindowViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;

        SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
        SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
        SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);

        var process = TargetAppData.Instance.TargetProcess;

        AppBarVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;

        if (process != null)
        {
            ApplicationName = process.ProcessName;
            ApplicationPid = process.Id;
            ApplicationIcon = TargetAppData.Instance.Icon;
            ApplicationHwnd = TargetAppData.Instance.HWnd;
        }

        CurrentSnapButtonText = IsSnapped ? _UnsnapButtonText : _SnapButtonText;

        _snapHelper = new();
    }

    partial void OnIsSnappedChanged(bool value)
    {
        CurrentSnapButtonText = IsSnapped ? _UnsnapButtonText : _SnapButtonText;
    }

    partial void OnBarOrientationChanged(Orientation value)
    {
        if (value == Orientation.Horizontal)
        {
            // If we were snapped, unsnap
            IsSnapped = false;
        }
        else
        {
            // Don't show expanded content in vertical mode
            ShowingExpandedContent = false;
        }
    }

    public void ResetBarWindowOnTop()
    {
        // If we're snapped to a target window, and that window loses and then regains focus,
        // we need to bring our window to the front also, to be in-sync. Otherwise, we can
        // end up with the target in the foreground, but our window partially obscured.
        // We set IsAlwaysOnTop to true to get it in foreground and then set to false,
        // this ensures we don't steal focus from target window and at the same time
        // bar window is not partially obscured.
        IsAlwaysOnTop = true;
        IsAlwaysOnTop = false;
    }

    [RelayCommand]
    public void SwitchLayout()
    {
        if (BarOrientation == Orientation.Horizontal)
        {
            BarOrientation = Orientation.Vertical;
        }
        else
        {
            BarOrientation = Orientation.Horizontal;
        }
    }

    public void UnsnapBarWindow()
    {
        _snapHelper.Unsnap();
        IsSnapped = false;
    }

    [RelayCommand]
    public void ToggleSnap()
    {
        if (IsSnapped)
        {
            UnsnapBarWindow();
        }
        else
        {
            // First need to be in a Vertical layout
            BarOrientation = Orientation.Vertical;
            _snapHelper.Snap();
            IsSnapped = true;
        }
    }

    [RelayCommand]
    public void ToggleExpandedContentVisibility()
    {
        if (!ShowingExpandedContent)
        {
            // First need to be in a horizontal layout to show expanded content
            BarOrientation = Orientation.Horizontal;
            ShowingExpandedContent = true;
        }
        else
        {
            ShowingExpandedContent = false;
        }
    }

    [RelayCommand]
    public void ProcessChooser()
    {
        ToggleExpandedContentVisibility();

        // And navigate to the appropriate page
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateTo(typeof(ProcessListPageViewModel));
    }

    [RelayCommand]
    public void ManageExternalToolsButton()
    {
        ToggleExpandedContentVisibility();

        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateToPiSettings(typeof(AdditionalToolsViewModel).FullName!);
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.HWnd))
        {
            _dispatcher.TryEnqueue(() =>
            {
                IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;

                // If snapped, retarget to the new window
                if (IsSnapped)
                {
                    _snapHelper.Unsnap();
                    _snapHelper.Snap();
                }
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            var process = TargetAppData.Instance.TargetProcess;

            _dispatcher.TryEnqueue(() =>
            {
                // The App status bar is only visibile if we're attached to a process
                AppBarVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;

                if (process is not null)
                {
                    ApplicationPid = process.Id;
                    ApplicationName = process.ProcessName;
                }
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.Icon))
        {
            SoftwareBitmapSource? icon = TargetAppData.Instance?.Icon;

            _dispatcher.TryEnqueue(() =>
            {
                ApplicationIcon = icon;
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            // Grey ourselves out if the app has exited?
        }
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.SystemCpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemRamUsageInGB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                // Convert from bytes to GBs
                SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemDiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                AppCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
            });
        }
    }

    public void ExternalToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton)
        {
            if (clickedButton.Tag is ExternalTool tool)
            {
                InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
            }
        }
    }

    public void ManageExternalToolsButton_ExternalToolLaunchRequest(object sender, ExternalTool tool)
    {
        InvokeTool(tool, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd);
    }

    private void InvokeTool(ExternalTool tool, int? pid, HWND hWnd)
    {
        var process = tool.Invoke(pid, hWnd);
        if (process is null)
        {
            // A ContentDialog only renders in the space its parent occupies. Since the parent is a narrow
            // bar, the dialog doesn't have enough space to render. So, we'll use MessageBox to display errors.
            Windows.Win32.PInvoke.MessageBox(
                HWND.Null, // ThisHwnd,
                string.Format(CultureInfo.CurrentCulture, _errorMessageText, tool.Executable),
                _errorTitleText,
                Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }
}
