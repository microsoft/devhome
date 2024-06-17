// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common;
using DevHome.Experiments.ViewModels;

namespace DevHome.Experiments.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestExperimentPage : ToolPage
{
    public override string DisplayName =>
        new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader(
            "DevHome.Experiments.pri",
            "DevHome.Experiments/Resources").GetString("NavigationPane/Content");

    public TestExperimentViewModel ViewModel
    {
        get;
    }

    public TestExperimentPage()
    {
        ViewModel = new TestExperimentViewModel();
        InitializeComponent();
    }
}
