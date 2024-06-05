// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;

namespace DevHome.PI.ViewModels;

public partial class ExpandedViewControlViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue dispatcher;

    [ObservableProperty]
    private Visibility perfMarkersVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string applicationPid = string.Empty;

    [ObservableProperty]
    private string applicationName = string.Empty;

    [ObservableProperty]
    private string cpuUsage = string.Empty;

    [ObservableProperty]
    private string ramUsage = string.Empty;

    [ObservableProperty]
    private string diskUsage = string.Empty;

    [ObservableProperty]
    private string clipboardContentsHex = string.Empty;

    [ObservableProperty]
    private string clipboardContentsDec = string.Empty;

    [ObservableProperty]
    private string clipboardContentsCode = string.Empty;

    [ObservableProperty]
    private string clipboardContentsHelp = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string settingsHeader = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PageNavLink> links;

    [ObservableProperty]
    private int selectedNavLinkIndex = 0;

    public INavigationService NavigationService { get; }

    private readonly PageNavLink appDetailsNavLink;
    private readonly PageNavLink resourceUsageNavLink;
    private readonly PageNavLink modulesNavLink;
    private readonly PageNavLink watsonNavLink;
    private readonly PageNavLink winLogsNavLink;
    private readonly PageNavLink processListNavLink;
    private readonly PageNavLink insightsNavLink;

    public ExpandedViewControlViewModel()
    {
        dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        PerfCounters.Instance.PropertyChanged += PerfCounterHelper_PropertyChanged;
        ClipboardMonitor.Instance.PropertyChanged += Clipboard_PropertyChanged;

        appDetailsNavLink = new PageNavLink("\uE71D", CommonHelper.GetLocalizedString("AppDetailsTextBlock/Text"), typeof(AppDetailsPageViewModel));
        resourceUsageNavLink = new PageNavLink("\uE950", CommonHelper.GetLocalizedString("ResourceUsageHeaderTextBlock/Text"), typeof(ResourceUsagePageViewModel));
        modulesNavLink = new PageNavLink("\uE74C", CommonHelper.GetLocalizedString("ModulesHeaderTextBlock/Text"), typeof(ModulesPageViewModel));
        watsonNavLink = new PageNavLink("\uE7BA", CommonHelper.GetLocalizedString("WatsonsHeaderTextBlock/Text"), typeof(WatsonPageViewModel));
        winLogsNavLink = new PageNavLink("\uE7C4", CommonHelper.GetLocalizedString("WinLogsHeaderTextBlock/Text"), typeof(WinLogsPageViewModel));
        processListNavLink = new PageNavLink("\uE8FD", CommonHelper.GetLocalizedString("ProcessListHeaderTextBlock/Text"), typeof(ProcessListPageViewModel));
        insightsNavLink = new PageNavLink("\uE946", CommonHelper.GetLocalizedString("InsightsHeaderTextBlock/Text"), typeof(InsightsPageViewModel));

        links = new();
        AddPagesIfNecessary(TargetAppData.Instance.TargetProcess);

        // Initial values
        CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormat", PerfCounters.Instance.CpuUsage);
        RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormat", PerfCounters.Instance.RamUsageInMB);
        DiskUsage = CommonHelper.GetLocalizedString("DiskPerfTextFormat", PerfCounters.Instance.DiskUsage);
        NavigationService = Application.Current.GetService<INavigationService>();

        SettingsHeader = CommonHelper.GetLocalizedString("SettingsToolHeaderTextBlock/Text");
    }

    private void PerfCounterHelper_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PerfCounters.CpuUsage))
        {
            dispatcher.TryEnqueue(() =>
            {
                CpuUsage = CommonHelper.GetLocalizedString("CpuPerfTextFormat", PerfCounters.Instance.CpuUsage);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.RamUsageInMB))
        {
            dispatcher.TryEnqueue(() =>
            {
                RamUsage = CommonHelper.GetLocalizedString("MemoryPerfTextFormat", PerfCounters.Instance.RamUsageInMB);
            });
        }
        else if (e.PropertyName == nameof(PerfCounters.DiskUsage))
        {
            dispatcher.TryEnqueue(() =>
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

            dispatcher.TryEnqueue(() =>
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

            dispatcher.TryEnqueue(() =>
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
                dispatcher.TryEnqueue(() =>
                {
                    Title = CommonHelper.GetLocalizedString("TerminatedText", ApplicationName);
                });
            }
        }
    }

    private void Clipboard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var clipboardContents = ClipboardMonitor.Instance.Contents;
        dispatcher.TryEnqueue(() =>
        {
            ClipboardContentsHex = clipboardContents.Hex;
            ClipboardContentsDec = clipboardContents.Dec;
            ClipboardContentsCode = clipboardContents.Code;
            ClipboardContentsHelp = clipboardContents.Help;
        });
    }

    private void AddPagesIfNecessary(Process? process)
    {
        if (!Links.Contains(processListNavLink))
        {
            Links.Add(processListNavLink);
        }

        // If App Details is missing, add all other pages.
        if (!Links.Contains(appDetailsNavLink))
        {
            if (process is not null)
            {
                Links.Insert(0, appDetailsNavLink);
                Links.Insert(1, resourceUsageNavLink);
                Links.Insert(2, modulesNavLink);
                Links.Insert(3, watsonNavLink);
                Links.Insert(4, winLogsNavLink);
                Links.Insert(6, insightsNavLink);
            }
        }
    }

    private void RemoveAppSpecificPages()
    {
        // First navigate to ProcessListPage, then remove all other pages.
        SelectedNavLinkIndex = Links.IndexOf(processListNavLink);

        Links.Remove(appDetailsNavLink);
        Links.Remove(resourceUsageNavLink);
        Links.Remove(modulesNavLink);
        Links.Remove(watsonNavLink);
        Links.Remove(winLogsNavLink);
        Links.Remove(insightsNavLink);
    }

    public void NavigateTo(Type viewModelType)
    {
        for (var i = 0; i < Links.Count; i++)
        {
            var link = Links[i];
            if (link.PageViewModel == viewModelType)
            {
                SelectedNavLinkIndex = i;
                break;
            }
        }
    }

    public void Navigate()
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        navigationService.NavigateTo(Links[SelectedNavLinkIndex]?.PageViewModel?.FullName!);
    }

    public void NavigateToSettings(string viewModelType)
    {
        // Because the Settings item isn't part of our NavLink list, when the user selects Settings,
        // we need to move the list selection so that when they subsequently select an item from
        // the NavLinks, we'll navigate to the correct page even if that was the previously-selected item.
        if (SelectedNavLinkIndex == 0)
        {
            SelectedNavLinkIndex = Links.Count - 1;
        }
        else
        {
            SelectedNavLinkIndex = 0;
        }

        var navigationService = Application.Current.GetService<INavigationService>();
        var mainSettingsPage = typeof(SettingsPageViewModel).FullName!;
        navigationService.NavigateTo(mainSettingsPage);
        if (!string.Equals(mainSettingsPage, viewModelType, StringComparison.OrdinalIgnoreCase))
        {
            navigationService.NavigateTo(viewModelType);
        }
    }
}
