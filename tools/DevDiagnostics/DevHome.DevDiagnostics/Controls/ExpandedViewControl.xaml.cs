// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.DevDiagnostics.Controls;

public sealed partial class ExpandedViewControl : UserControl
{
    private readonly ExpandedViewControlViewModel _viewModel = new();

    public ExpandedViewControl()
    {
        InitializeComponent();
        _viewModel.NavigationService.Frame = PageFrame;
    }

    public Frame GetPageFrame()
    {
        return PageFrame;
    }

    public void NavigateTo(Type viewModelType)
    {
        _viewModel.NavigateTo(viewModelType);
    }

    public void NavigateToSettings(string viewModelType)
    {
        _viewModel.NavigateToSettings(viewModelType);
    }

    private void GridSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }
}
