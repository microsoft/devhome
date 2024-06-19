// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class WatsonPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<WatsonReport> _displayedReports = [];

    [ObservableProperty]
    private string _watsonInfoText;

    public WatsonPageViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;

        _watsonInfoText = string.Empty;

        ((INotifyCollectionChanged)WatsonHelper.Instance.WatsonReports).CollectionChanged += WatsonOutput_CollectionChanged;

        PopulateCurrentLogs(TargetAppData.Instance.TargetProcess);
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            PopulateCurrentLogs(TargetAppData.Instance.TargetProcess);
        }
    }

    private void WatsonOutput_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            _dispatcher.TryEnqueue(() =>
            {
                foreach (WatsonReport report in e.NewItems)
                {
                    // Provide filtering if needed
                    DisplayedReports.Add(report);
                }
            });
        }
    }

    private void PopulateCurrentLogs(Process? process)
    {
        _dispatcher.TryEnqueue(() =>
        {
            DisplayedReports.Clear();

            // Get all existing reports
            foreach (var report in WatsonHelper.Instance.WatsonReports)
            {
                // Provide filtering if needed
                DisplayedReports.Add(report);
            }
        });
    }
}
