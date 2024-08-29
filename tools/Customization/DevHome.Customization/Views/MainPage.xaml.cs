// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.Customization.Views;

public sealed partial class MainPage : ToolPage
{
    public MainPageViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = Application.Current.GetService<MainPageViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.NavigationMode != NavigationMode.Back)
        {
            var parameter = e.Parameter?.ToString();
            if (parameter != null && parameter.Equals("ShowFileExplorer", StringComparison.OrdinalIgnoreCase))
            {
                Application.Current.GetService<DispatcherQueue>().TryEnqueue(ViewModel.NavigateToFileExplorerPage);
            }
        }
    }
}
