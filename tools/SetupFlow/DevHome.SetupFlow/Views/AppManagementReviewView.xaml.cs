// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class AppManagementReviewView : UserControl
{
    public AppManagementReviewViewModel ViewModel => (AppManagementReviewViewModel)DataContext;

    public AppManagementReviewView()
    {
        this.InitializeComponent();
    }
}
