// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;
public sealed partial class RepoConfigReviewView : UserControl
{
    public RepoConfigReviewViewModel ViewModel => (RepoConfigReviewViewModel)DataContext;

    public RepoConfigReviewView()
    {
        this.InitializeComponent();
    }
}
