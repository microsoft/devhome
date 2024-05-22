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
    private const string UnsnapButtonText = "\ue89f";
    private const string SnapButtonText = "\ue8a0";

    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    private readonly ObservableCollection<Button> externalTools = [];

    [ObservableProperty]
    private string systemCpuUsage = string.Empty;

    [ObservableProperty]
    private string systemRamUsage = string.Empty;

    [ObservableProperty]
    private string systemDiskUsage = string.Empty;

    [ObservableProperty]
    private bool isSnappingEnabled = false;

    [ObservableProperty]
    private string currentSnapButtonText = SnapButtonText;

    [ObservableProperty]
    private string appCpuUsage = string.Empty;

    [ObservableProperty]
    private Visibility appBarVisibility = Visibility.Visible;

    [ObservableProperty]
    private int applicationPid;

    [ObservableProperty]
    private SoftwareBitmapSource? applicationIcon;

    [ObservableProperty]
    private Orientation barOrientation = Orientation.Horizontal;

    [ObservableProperty]
    private bool isSnapped;

    [ObservableProperty]
    private bool showingExpandedContent;

    public BarWindowViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

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
            ApplicationPid = process.Id;
            ApplicationIcon = TargetAppData.Instance.Icon;
        }

        CurrentSnapButtonText = IsSnapped ? UnsnapButtonText : SnapButtonText;
    }

    partial void OnIsSnappedChanged(bool value)
    {
        CurrentSnapButtonText = IsSnapped ? UnsnapButtonText : SnapButtonText;
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

            dispatcher.TryEnqueue(() =>
            {
                // The App status bar is only visibile if we're attached to a process
                AppBarVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;

                if (process is not null)
                {
                    ApplicationPid = process.Id;
                }
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.Icon))
        {
            SoftwareBitmapSource? icon = TargetAppData.Instance?.Icon;

            dispatcher.TryEnqueue(() =>
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
            dispatcher.TryEnqueue(() =>
            {
                SystemCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.SystemCpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemRamUsageInGB))
        {
            dispatcher.TryEnqueue(() =>
            {
                // Convert from bytes to GBs
                SystemRamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormatNoLabelGB", PerfCounters.Instance.SystemRamUsageInGB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.SystemDiskUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                SystemDiskUsage = CommonHelper.GetLocalizedString("DiskPerfPercentUsageTextFormatNoLabel", PerfCounters.Instance.SystemDiskUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                AppCpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormatNoLabel", PerfCounters.Instance.CpuUsage);
            });
        }
    }
}
