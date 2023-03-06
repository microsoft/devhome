// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.AppManagement.Views;
public sealed partial class AppManagementReviewView : UserControl
{
    public AppManagementReviewViewModel ViewModel => (AppManagementReviewViewModel)DataContext;

    public AppManagementReviewView()
    {
        this.InitializeComponent();
    }
}
