// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.AppManagement.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.AppManagement.Views;

public sealed partial class AppManagementView : UserControl
{
    public AppManagementView()
    {
        this.InitializeComponent();
    }

    public AppManagementViewModel ViewModel => (AppManagementViewModel)this.DataContext;
}
