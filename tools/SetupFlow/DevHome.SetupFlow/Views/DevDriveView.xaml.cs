// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.SetupFlow.Views;

public sealed partial class DevDriveView : UserControl
{
    public DevDriveViewModel ViewModel { get; }

    public DevDriveView(DevDriveViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        this.Loaded += (_, _) =>
        {
            var option = new FindNextElementOptions();
            option.SearchRoot = GoToLearnMoreHyperlink;
            var focusableElement = FocusManager.FindNextElement(FocusNavigationDirection.Up, option);
            focusableElement.SetValue(IsTabStopProperty, false);
        };
    }
}
