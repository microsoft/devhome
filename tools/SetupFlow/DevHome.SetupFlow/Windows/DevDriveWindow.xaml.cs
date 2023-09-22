// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Windows;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;

namespace DevHome.SetupFlow.Windows;
public sealed partial class DevDriveWindow : SecondaryWindow
{
    public DevDriveWindow(DevDriveViewModel viewModel)
    {
        WindowContent = new DevDriveView(viewModel);
        this.InitializeComponent();
    }
}
