// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common;
using Tools.SampleTool.ViewModels;

namespace Tools.SampleTool.Views;

public partial class SampleToolPage : ToolPage
{
    public override string DisplayName =>
        new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader(
            "DevHome.SampleTool.pri",
            "DevHome.SampleTool/Resources").GetString("NavigationPane/Content");

    public SampleToolViewModel ViewModel
    {
        get;
    }

    public SampleToolPage()
    {
        ViewModel = new SampleToolViewModel();
        InitializeComponent();
    }
}
