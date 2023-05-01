// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common;
using Tools.SampleTool.ViewModels;

namespace Tools.SampleTool.Views;

public partial class SampleToolPage : ToolPage
{
    public override string ShortName => "SampleTool";

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
