// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using DevHome.Common;
using DevHome.Experiments.ViewModels;

namespace DevHome.Experiments.Views;


public partial class ExperimentsPage : ToolPage
{
    public override string ShortName => "Experiments";

    public ExperimentsViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<ExperimentalFeature> ExperimentalFeatures
    {
        get;
    } = new ObservableCollection<ExperimentalFeature>();

    public ExperimentsPage()
    {
        ViewModel = new ExperimentsViewModel();
        InitializeComponent();
    }
}
