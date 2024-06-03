// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Windows;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;

namespace DevHome.SetupFlow.Windows;

public sealed partial class DevDriveWindow : SecondaryWindow
{
    public DevDriveWindow(DevDriveViewModel viewModel)
        : base(new DevDriveView(viewModel))
    {
        this.InitializeComponent();
    }
}
