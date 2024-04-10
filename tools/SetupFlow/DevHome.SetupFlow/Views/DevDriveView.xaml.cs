// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class DevDriveView : UserControl
{
    public DevDriveViewModel ViewModel { get; }

    public DevDriveView(DevDriveViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
    }
}
