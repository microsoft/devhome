// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.DevVolume.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.DevVolume.Views;

public sealed partial class DevVolumeView : UserControl
{
    public DevVolumeView()
    {
        this.InitializeComponent();
    }

    public DevVolumeViewModel ViewModel => (DevVolumeViewModel)this.DataContext;
}
