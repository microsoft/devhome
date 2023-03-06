// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.Loading.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Loading.Views;

public sealed partial class LoadingView : UserControl
{
    public LoadingView()
    {
        this.InitializeComponent();
    }

    public LoadingViewModel ViewModel => (LoadingViewModel)this.DataContext;
}
