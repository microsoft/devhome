// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
