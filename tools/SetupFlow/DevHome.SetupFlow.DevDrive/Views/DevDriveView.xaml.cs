// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.DevDrive.Views;

public sealed partial class DevDriveView : Page
{
    public DevDriveView(DevDriveViewModel viewModel)
    {
        this.InitializeComponent();
        DevDriveViewControl.DataContext = viewModel;
    }

    public DevDriveViewModel ViewModel => (DevDriveViewModel)DevDriveViewControl.DataContext;

    public Grid TitleBar => AppTitleBar;

    public void UpdateTitleBarTextForeground(SolidColorBrush brush)
    {
        AppTitleBarText.Foreground = brush;
    }
}
