// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class ReviewView : UserControl
{
    public ReviewView()
    {
        this.InitializeComponent();
    }

    public ReviewViewModel ViewModel => (ReviewViewModel)this.DataContext;
}
