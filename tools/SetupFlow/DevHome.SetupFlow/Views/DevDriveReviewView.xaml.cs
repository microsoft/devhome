// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class DevDriveReviewView : UserControl
{
    public DevDriveReviewView()
    {
        this.InitializeComponent();
    }

    public DevDriveReviewViewModel ViewModel => (DevDriveReviewViewModel)DataContext;
}
