// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.UI.Xaml;
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
