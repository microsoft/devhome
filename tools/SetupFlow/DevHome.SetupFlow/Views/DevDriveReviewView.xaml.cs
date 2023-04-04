// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.DevDrive.Views;
public sealed partial class DevDriveReviewView : UserControl
{
    public DevDriveReviewView()
    {
        this.InitializeComponent();
    }

    public DevDriveReviewViewModel ViewModel => (DevDriveReviewViewModel)DataContext;
}
