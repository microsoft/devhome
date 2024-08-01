// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Repositories.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Repositories.Views;

public sealed partial class RepositoriesMainPageView : ToolPage
{
    public RepositoriesMainPageViewModel ViewModel { get; }

    public RepositoriesMainPageView()
    {
        ViewModel = Application.Current.GetService<RepositoriesMainPageViewModel>();
        this.InitializeComponent();
    }
}
