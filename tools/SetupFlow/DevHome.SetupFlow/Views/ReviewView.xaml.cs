// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.Review.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Review.Views;

public sealed partial class ReviewView : UserControl
{
    public ReviewView()
    {
        this.InitializeComponent();
    }

    public ReviewViewModel ViewModel => (ReviewViewModel)this.DataContext;
}
