// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public partial class SetupTargetReviewView : UserControl
{
    public SetupTargetReviewViewModel ViewModel => (SetupTargetReviewViewModel)DataContext;

    public SetupTargetReviewView()
    {
        this.InitializeComponent();
    }

    private void UserControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            _ = ViewModel.LoadViewModelContentAsync();
        }
    }
}
