// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.Diagnostics.Tracing.StackSources;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32.Foundation;

namespace DevHome.PI.ViewModels;

public partial class BarWindowViewModel : ObservableObject
{
    private const string _UnsnapButtonText = "\ue89f";
    private const string _SnapButtonText = "\ue8a0";

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly ObservableCollection<Button> _externalTools = [];

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
    private string? _applicationName;

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
        }

        CurrentSnapButtonText = IsSnapped ? _UnsnapButtonText : _SnapButtonText;
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

    [RelayCommand]
    public void SwitchLayoutCommand()
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

    [RelayCommand]
    public void PerformSnapCommand()
    {
        if (IsSnapped)
        {
            IsSnapped = false;
        }
        else
        {
            // First need to be in a Vertical layout
            BarOrientation = Orientation.Vertical;
            IsSnapped = true;
        }
    }

    [RelayCommand]
    public void ShowBigWindowCommand()
    {
        if (!ShowingExpandedContent)
        {
            // First need to be in a horizontal layout
            BarOrientation = Orientation.Horizontal;
            ShowingExpandedContent = true;
        }
        else
        {
            ShowingExpandedContent = false;
        }
    }

    [RelayCommand]
    public void ProcessChooserCommand()
    {
        // Need to be in a horizontal layout
        BarOrientation = Orientation.Horizontal;

        // And show expanded content
        ShowingExpandedContent = true;

        // And navigate to the appropriate page
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.NavigateTo(typeof(ProcessListPageViewModel));
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.HWnd))
        {
            IsSnappingEnabled = TargetAppData.Instance.HWnd != HWND.Null;
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
}
