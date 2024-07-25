// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;

namespace DevHome.PI.ViewModels;

public partial class InsightsPageViewModel : ObservableObject
{
    private readonly BarWindowViewModel _barWindowViewModel;
    private Process? _targetProcess;

    [ObservableProperty]
    private ObservableCollection<Insight> _insightsList;

    [ObservableProperty]
    private int _unreadCount;

    public InsightsPageViewModel()
    {
        _barWindowViewModel = Application.Current.GetService<BarWindowViewModel>();

        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        _insightsList = [];

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (_targetProcess != process)
        {
            _targetProcess = process;
            InsightsList.Clear();
            UnreadCount = 0;
            _barWindowViewModel.UpdateUnreadInsightsCount(0);
        }
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TargetAppData.TargetProcess))
        {
            if (TargetAppData.Instance.TargetProcess is not null)
            {
                UpdateTargetProcess(TargetAppData.Instance.TargetProcess);
            }
        }
    }

    internal void AddInsight(Insight insight)
    {
        insight.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Insight.HasBeenRead))
            {
                UnreadCount = InsightsList.Count(insight => !insight.HasBeenRead);
                _barWindowViewModel.UpdateUnreadInsightsCount(UnreadCount);
            }
        };

        UnreadCount++;
        _barWindowViewModel.UpdateUnreadInsightsCount(UnreadCount);
        InsightsList.Add(insight);
    }
}
