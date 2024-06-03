// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class FileExplorerPage : Page
{
    public FileExplorerViewModel ViewModel
    {
        get;
    }

    public FileExplorerPage()
    {
        ViewModel = Application.Current.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
    }
}
