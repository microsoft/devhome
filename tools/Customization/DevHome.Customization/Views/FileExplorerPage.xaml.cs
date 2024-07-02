// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Customization.Views;

public sealed partial class FileExplorerPage : ToolPage
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
