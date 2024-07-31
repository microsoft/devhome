// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using Microsoft.UI.Xaml;

namespace DevHome.PI.ViewModels;

public partial class ExpandedViewControlViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private Visibility _perfMarkersVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string _applicationPid = string.Empty;

    [ObservableProperty]
    private string _applicationName = string.Empty;

    [ObservableProperty]
    private string _cpuUsage = string.Empty;

    [ObservableProperty]
    private string _ramUsage = string.Empty;

    [ObservableProperty]
    private string _diskUsage = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsHex = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsDec = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsCode = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsHelp = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PageNavLink> _links;

    [ObservableProperty]
    private int _selectedNavLinkIndex = 0;

    [ObservableProperty]
    private bool _applyAppFiltering;

    private string? _additionalNavigationInfo;

    public INavigationService NavigationService { get; }

    private readonly PageNavLink _appDetailsNavLink;
    private readonly PageNavLink _resourceUsageNavLink;
    private readonly PageNavLink _modulesNavLink;
    private readonly PageNavLink _werNavLink;
    private readonly PageNavLink _winLogsNavLink;
    private readonly PageNavLink _processListNavLink;
    private readonly PageNavLink _insightsNavLink;
    private readonly PageNavLink _settingsNavLink;

    public ExpandedViewControlViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;
        ClipboardMonitor.Instance.PropertyChanged += Clipboard_PropertyChanged;

        _appDetailsNavLink = new PageNavLink("\uE71D", CommonHelper.GetLocalizedString("AppDetailsTextBlock/Text"), typeof(AppDetailsPageViewModel));
        _resourceUsageNavLink = new PageNavLink("\uE950", CommonHelper.GetLocalizedString("ResourceUsageHeaderTextBlock/Text"), typeof(ResourceUsagePageViewModel));
        _modulesNavLink = new PageNavLink("\uE74C", CommonHelper.GetLocalizedString("ModulesHeaderTextBlock/Text"), typeof(ModulesPageViewModel));
        _werNavLink = new PageNavLink("\uE7BA", CommonHelper.GetLocalizedString("WERHeaderTextBlock/Text"), typeof(WERPageViewModel));
        _winLogsNavLink = new PageNavLink("\uE7C4", CommonHelper.GetLocalizedString("WinLogsHeaderTextBlock/Text"), typeof(WinLogsPageViewModel));
        _processListNavLink = new PageNavLink("\uE8FD", CommonHelper.GetLocalizedString("ProcessListHeaderTextBlock/Text"), typeof(ProcessListPageViewModel));
        _insightsNavLink = new PageNavLink("\uE946", CommonHelper.GetLocalizedString("InsightsHeaderTextBlock/Text"), typeof(InsightsPageViewModel));
        _settingsNavLink = new PageNavLink("\uE713", CommonHelper.GetLocalizedString("SettingsToolHeaderTextBlock/Text"), typeof(SettingsPageViewModel));

        _links = [];

        ApplyAppFiltering = Settings.Default.ApplyAppFilteringToData;
        AddPagesIfNecessary(TargetAppData.Instance.TargetProcess);

        // Initial values
        CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormat", PerfCounters.Instance.CpuUsage);
        RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormat", PerfCounters.Instance.RamUsageInMB);
        DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormat", PerfCounters.Instance.DiskUsage);
        NavigationService = Application.Current.GetService<INavigationService>();
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormat", PerfCounters.Instance.CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            _dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormat", PerfCounters.Instance.RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            _dispatcher.TryEnqueue(() =>
            {
                DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormat", PerfCounters.Instance.DiskUsage);
            });
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            var process = TargetAppData.Instance.TargetProcess;

            _dispatcher.TryEnqueue(() =>
            {
                // The App status bar is only visibile if we're attached to a process
                PerfMarkersVisibility = process is null ? Visibility.Collapsed : Visibility.Visible;
                var pid = process?.Id ?? 0;
                ApplicationPid = CommonHelper.GetLocalizedString("TargetAppPidTextFormat", pid);

                ApplicationName = process?.ProcessName ?? string.Empty;
                Title = process?.ProcessName ?? string.Empty;

                if (process is null)
                {
                    RemoveAppSpecificPages();
                }
                else
                {
                    AddPagesIfNecessary(process);
                }
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.AppName))
        {
            var newAppName = TargetAppData.Instance.AppName;

            _dispatcher.TryEnqueue(() =>
            {
                ApplicationName = newAppName;
                Title = newAppName;
            });
        }
        else if (e.PropertyName == nameof(TargetAppData.HasExited))
        {
            // There's a race where the user could switch to a different app
            // before we get the HasExited event, so we check if the exited process
            // is the same as the one we're now tracking.
            var process = TargetAppData.Instance.TargetProcess;
            if (process != null && process.HasExited)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Title = CommonHelper.GetLocalizedString("TerminatedText", ApplicationName);
                });
            }
        }
    }

    private void Clipboard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var clipboardContents = ClipboardMonitor.Instance.Contents;
        _dispatcher.TryEnqueue(() =>
        {
            ClipboardContentsHex = clipboardContents.Hex;
            ClipboardContentsDec = clipboardContents.Dec;
            ClipboardContentsCode = clipboardContents.Code;
            ClipboardContentsHelp = clipboardContents.Help;
        });
    }

    private void AddPagesIfNecessary(Process? process)
    {
        if (!Links.Contains(_processListNavLink))
        {
            Links.Add(_processListNavLink);
            Links.Add(_werNavLink);
            Links.Add(_insightsNavLink);
            Links.Add(_settingsNavLink);
        }

        // If App Details is missing, add all other pages.
        if (!Links.Contains(_appDetailsNavLink))
        {
            if (process is not null)
            {
                Links.Insert(0, _appDetailsNavLink);
                Links.Insert(1, _resourceUsageNavLink);
                Links.Insert(2, _modulesNavLink);

                // Process List #3
                // WER #4
                Links.Insert(5, _winLogsNavLink);

                // Insights #6;
            }
        }
    }

    private void RemoveAppSpecificPages()
    {
        // First navigate to ProcessListPage, then remove all other pages.
        SelectedNavLinkIndex = Links.IndexOf(_processListNavLink);

        Links.Remove(_appDetailsNavLink);
        Links.Remove(_resourceUsageNavLink);
        Links.Remove(_modulesNavLink);
        Links.Remove(_winLogsNavLink);
    }

    public void NavigateTo(Type viewModelType)
    {
        for (var i = 0; i < Links.Count; i++)
        {
            var link = Links[i];
            if (link.PageViewModel == viewModelType)
            {
                if (SelectedNavLinkIndex == i)
                {
                    // Navigation between pages is a bit convoluted, as we set SelectedNavLinkIndex, which triggers a change
                    // event on a ListView that calls Navigate() to perform the actual navigation.
                    //
                    // If we're trying to navigate to the same Listview item, we need to call Navigate() directly. ListViewItems != Pages (for example, Settings
                    // is a ListView Item, but there are several pages under Settings). If we end up navigating to the same page,
                    // the NavService will no-op it
                    Navigate();
                }
                else
                {
                    SelectedNavLinkIndex = i;
                }

                break;
            }
        }
    }

    public void Navigate()
    {
        var navigationService = Application.Current.GetService<INavigationService>();

        if (_additionalNavigationInfo is not null)
        {
            Debug.Assert(Links[SelectedNavLinkIndex] == _settingsNavLink, "Addition Nav Info currently only supported for Settings");
            navigationService.NavigateTo(_additionalNavigationInfo);
            _additionalNavigationInfo = null;
            return;
        }
        else
        {
            navigationService.NavigateTo(Links[SelectedNavLinkIndex]?.PageViewModel?.FullName!);
        }
    }

    public void NavigateToSettings(string viewModelType)
    {
        _additionalNavigationInfo = viewModelType;
        NavigateTo(typeof(SettingsPageViewModel));
    }

    [RelayCommand]
    public void ApplyAppFilteringToData()
    {
        // This command is called before the control has had a chance to toggle the ApplyAppFiltering setting.
        Settings.Default.ApplyAppFilteringToData = !ApplyAppFiltering;
        Settings.Default.Save();
    }
}
