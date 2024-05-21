// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.PI.Controls;

public partial class AppStatusBar : UserControl
{
    private readonly AppStatusBarViewModel viewModel = new();

    public AppStatusBar()
    {
        this.InitializeComponent();
    }

    public void Initialize(BarWindow barWindow)
    {
        viewModel.SetBarWindow(barWindow);
    }
}
