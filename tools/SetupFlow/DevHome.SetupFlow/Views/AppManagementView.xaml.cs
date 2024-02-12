// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class AppManagementView : UserControl
{
    public string Title => SetupShell.Title;

    public AppManagementView()
    {
        this.InitializeComponent();
    }

    public AppManagementViewModel ViewModel => (AppManagementViewModel)this.DataContext;

    public void SetHeaderVisibility(Visibility visibility)
    {
        SetupShell.HeaderVisibility = visibility;
        SearchBox.Visibility = visibility;
    }
}
