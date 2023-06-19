// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public partial class SetupFlowPage : ToolPage
{
    public override string ShortName => "SetupFlow";

    public SetupFlowViewModel ViewModel { get; }

    public SetupFlowPage()
    {
        ViewModel = Application.Current.GetService<SetupFlowViewModel>();
        InitializeComponent();
    }
}
