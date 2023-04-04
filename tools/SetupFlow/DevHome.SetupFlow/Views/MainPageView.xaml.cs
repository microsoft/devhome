// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class MainPageView : UserControl
{
    public MainPageView()
    {
        this.InitializeComponent();
    }

    public MainPageViewModel ViewModel => (MainPageViewModel)this.DataContext;
}
