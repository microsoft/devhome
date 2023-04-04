// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl
{
    public SummaryView()
    {
        this.InitializeComponent();
    }

    public SummaryViewModel ViewModel => (SummaryViewModel)this.DataContext;
}
