// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.RepositoryManagement.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.RepositoryManagement.Views;

public sealed partial class RepositoryManagementMainPageView : ToolPage
{
    public RepositoryManagementMainPageViewModel ViewModel { get; }

    public RepositoryManagementMainPageView()
    {
        ViewModel = Application.Current.GetService<RepositoryManagementMainPageViewModel>();
        this.InitializeComponent();
    }
}
