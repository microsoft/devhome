// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class DeveloperFileExplorerView : UserControl
{
    public DeveloperFileExplorerViewModel ViewModel
    {
        get;
    }

    public DeveloperFileExplorerView()
    {
        InitializeComponent();

        ViewModel = Application.Current.GetService<DeveloperFileExplorerViewModel>();
    }
}
