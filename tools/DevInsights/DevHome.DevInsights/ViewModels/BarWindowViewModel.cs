// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Helpers;
using DevHome.DevInsights.Models;
using DevHome.DevInsights.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.Graphics;
using Windows.System;
using Windows.Win32.Foundation;

namespace DevHome.DevInsights.ViewModels;

public partial class BarWindowViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(BarWindowViewModel));

    private const string UnsnapButtonText = "\ue89f";
    private const string SnapButtonText = "\ue8a0";

    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");
    private readonly string _unsnapToolTip = CommonHelper.GetLocalizedString("UnsnapToolTip");
    private readonly string _snapToolTip = CommonHelper.GetLocalizedString("SnapToolTip");
    private readonly string _expandToolTip = CommonHelper.GetLocalizedString("SwitchToLargeLayoutToolTip");
    private readonly string _collapseToolTip = CommonHelper.GetLocalizedString("SwitchToSmallLayoutToolTip");

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;
    private readonly ExternalToolsHelper _externalToolsHelper;

    private readonly ObservableCollection<Button> _externalTools = [];
    private readonly SnapHelper _snapHelper;
    private readonly DIInsightsService _insightsService;

    [ObservableProperty]
    private string _systemCpuUsage = string.Empty;

    [ObservableProperty]
    private string _systemRamUsage = string.Empty;

    [ObservableProperty]
    private string _systemDiskUsage = string.Empty;

    [ObservableProperty]
    private bool _isSnappingEnabled = false;

    [ObservableProperty]
    private string _currentSnapButtonText = SnapButtonText;

    [ObservableProperty]
    private string _currentSnapToolTip;

    [ObservableProperty]
    private string _currentExpandToolTip;

    [ObservableProperty]
    private string _appCpuUsage = string.Empty;

    [ObservableProperty]
    private bool _isAppBarVisible = true;

    [ObservableProperty]
    private bool _isProcessChooserVisible = false;

    [ObservableProperty]
    private Visibility _externalToolSeparatorVisibility = Visibility.Collapsed;

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

    [ObservableProperty]
    private SizeInt32 _requestedWindowSize;

    [ObservableProperty]
    private int _unreadInsightsCount;

    [ObservableProperty]
    private double _insightsBadgeOpacity;

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

        // Show either the result chooser, or the app bar. Not both
        IsProcessChooserVisible = process is null;
        IsAppBarVisible = !IsProcessChooserVisible;
        if (IsAppBarVisible)
        {
            Debug.Assert(process is not null, "Process should not be null if we're showing the app bar");
            ApplicationName = process.ProcessName;
            ApplicationPid = process.Id;
            ApplicationIcon = TargetAppData.Instance.Icon;
            ApplicationHwnd = TargetAppData.Instance.HWnd;
        }

        CurrentSnapButtonText = IsSnapped ? UnsnapButtonText : SnapButtonText;
        CurrentSnapToolTip = IsSnapped ? _unsnapToolTip : _snapToolTip;
        CurrentExpandToolTip = ShowingExpandedContent ? _collapseToolTip : _expandToolTip;
        _snapHelper = new();

        _externalToolsHelper = Application.Current.GetService<ExternalToolsHelper>();
        ((INotifyCollectionChanged)_externalToolsHelper.FilteredExternalTools).CollectionChanged += FilteredExternalTools_CollectionChanged;
        FilteredExternalTools_CollectionChanged(null, null);

        _insightsService = Application.Current.GetService<DIInsightsService>();
        _insightsService.PropertyChanged += InsightsService_PropertyChanged;
    }

    partial void OnShowingExpandedContentChanged(bool value)
    {
        CurrentExpandToolTip = ShowingExpandedContent ? _collapseToolTip : _expandToolTip;
    }

    private void FilteredExternalTools_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs? e)
    {
        // Only show the separator if we're showing pinned tools
        ExternalToolSeparatorVisibility = _externalToolsHelper.FilteredExternalTools.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    partial void OnIsSnappedChanged(bool value)
    {
        CurrentSnapButtonText = IsSnapped ? UnsnapButtonText : SnapButtonText;
        CurrentSnapToolTip = IsSnapped ? _unsnapToolTip : _snapToolTip;
    }

    partial void OnBarOrientationChanged(Orientation value)
    {
        if (value == Orientation.Horizontal)
        {
            // If we were snapped, unsnap
            UnsnapBarWindow();
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
    public void RotateLayout()
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

    public void SnapBarWindow()
    {
        // First need to be in a Vertical layout
        BarOrientation = Orientation.Vertical;
        _snapHelper.Snap();
        IsSnapped = true;
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
            SnapBarWindow();
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
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateTo(typeof(ProcessListPageViewModel));
    }

    [RelayCommand]
    public void ManageExternalToolsButton()
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateToPiSettings(typeof(AdditionalToolsViewModel).FullName!);
    }

    [RelayCommand]
    public void DetachFromProcess()
    {
        TargetAppData.Instance.ClearAppData();
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
                // The App status bar is only visible if we're attached to a result
                IsAppBarVisible = process is not null;

                if (process is not null)
                {
                    ApplicationPid = process.Id;
                    ApplicationName = process.ProcessName;
                }

                // Conversely, the result chooser is only visible if we're not attached to a result
                IsProcessChooserVisible = process is null;
                UnreadInsightsCount = 0;
                InsightsBadgeOpacity = 0;
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

    private void InsightsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DIInsightsService.UnreadCount))
        {
            UnreadInsightsCount = _insightsService.UnreadCount;
            InsightsBadgeOpacity = UnreadInsightsCount > 0 ? 1 : 0;
        }
    }

    public void ManageExternalToolsButton_ExternalToolLaunchRequest(object sender, ExternalTool tool)
    {
        tool.Invoke();
    }

    [RelayCommand]
    public void LaunchAdvancedAppsPageInWindowsSettings()
    {
        _ = Launcher.LaunchUriAsync(new("ms-settings:advanced-apps"));
    }

    [RelayCommand]
    private void ShowInsightsPage()
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        Debug.Assert(barWindow != null, "BarWindow should not be null.");
        barWindow.NavigateTo(typeof(InsightsPageViewModel));
    }
}
