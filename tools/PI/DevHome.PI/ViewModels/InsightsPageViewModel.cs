// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class InsightsPageViewModel : ObservableObject
{
    private Process? targetProcess;

    [ObservableProperty]
    private ObservableCollection<Insight> insightsList;

    public InsightsPageViewModel()
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
        insightsList = [];

        var process = TargetAppData.Instance.TargetProcess;
        if (process is not null)
        {
            UpdateTargetProcess(process);
        }
    }

    public void UpdateTargetProcess(Process process)
    {
        if (targetProcess != process)
        {
            targetProcess = process;
            InsightsList.Clear();
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
        InsightsList.Add(insight);
    }
}
