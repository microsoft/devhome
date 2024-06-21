// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Views;
using Tools.SampleTool.ViewModels;

namespace Tools.SampleTool.Views;

public partial class SampleToolPage : ToolPage
{
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
