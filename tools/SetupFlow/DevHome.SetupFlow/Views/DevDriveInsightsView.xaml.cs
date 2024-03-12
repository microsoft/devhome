// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class DevDriveInsightsView : UserControl
{
    public DevDriveInsightsViewModel ViewModel => (DevDriveInsightsViewModel)this.DataContext;

    public DevDriveInsightsView()
    {
        this.InitializeComponent();
    }
}
