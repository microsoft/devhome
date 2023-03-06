// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.Common;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Tools.SampleTool.ViewModels;

namespace Tools.SampleTool.Views;

public partial class SampleToolPage : ToolPage
{
    public override string ShortName => "SampleTool";

    private readonly ILogger logger;

    public SampleToolViewModel ViewModel
    {
        get;
    }

    public SampleToolPage()
    {
        ViewModel = new SampleToolViewModel();
        InitializeComponent();
        this.logger = LoggerFactory.Get<ILogger>();

        this.logger.Log("PageLoad", LogLevel.Local, nameof(SampleToolPage));
    }
}
