using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Models;

// This class monitors for WER reports and runs analysis on them

public class WERAnalyzer
{
    private readonly WERHelper _werHelper;
    private readonly ObservableCollection<WERReportAnalysis> _werReportsAnalysis = [];

    public ReadOnlyObservableCollection<WERReportAnalysis> WERReportsAnalysis { get; private set; }

    public WERAnalyzer()
    {
        WERReportsAnalysis = new(_werReportsAnalysis);

        _werHelper = Application.Current.GetService<WERHelper>();
        ((INotifyCollectionChanged)_werHelper.WERReports).CollectionChanged += WER_CollectionChanged;

        PopulateCurrentLogs();
    }

    private void WER_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                ProcessWatsonList(e.NewItems.Cast<WERReport>().ToList());
            });
        }
    }

    private void PopulateCurrentLogs()
    {
        ThreadPool.QueueUserWorkItem((o) =>
        {
            ProcessWatsonList(_werHelper.WERReports.ToList<WERReport>());
        });
    }

    private void ProcessWatsonList(List<WERReport> reports)
    {
        List<WERReportAnalysis> reportsToAnalyze = new();

        // First publish all of these reports to our listeners. Then we'll go back and perform
        // analysis on them.
        foreach (var report in reports)
        {
            var reportAnalysis = new WERReportAnalysis(report);
            _werReportsAnalysis.Add(reportAnalysis);
            reportsToAnalyze.Add(reportAnalysis);
        }

        foreach (var reportAnalysis in reportsToAnalyze)
        {
            reportAnalysis.PerformAnalysis();
        }
    }
}
