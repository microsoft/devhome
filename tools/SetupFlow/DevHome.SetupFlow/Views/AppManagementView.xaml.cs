// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
