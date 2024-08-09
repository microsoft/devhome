// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SearchView : UserControl
{
    public SearchViewModel ViewModel => (SearchViewModel)DataContext;

    public SearchView()
    {
        this.InitializeComponent();
    }
}
