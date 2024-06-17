// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Windows.Input;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Utilities.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Utilities.Views;

public sealed partial class UtilitiesMainPageView : ToolPage
{
    public override string DisplayName => "Utilities";

    public UtilitiesMainPageViewModel ViewModel { get; }

    public ICommand OpenNewWindowCommand { get; private set; }

    public UtilitiesMainPageView()
    {
        ViewModel = Application.Current.GetService<UtilitiesMainPageViewModel>();
        this.InitializeComponent();
    }
}
