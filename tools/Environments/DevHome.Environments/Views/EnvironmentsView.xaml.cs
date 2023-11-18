// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Environments.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EnvironmentsView : ToolPage
{
    public override string ShortName => "Environments";

    public EnvironmentsViewModel ViewModel { get; set; }

    public EnvironmentsView()
    {
        ViewModel = Application.Current.GetService<EnvironmentsViewModel>();
        this.InitializeComponent();
    }
}
