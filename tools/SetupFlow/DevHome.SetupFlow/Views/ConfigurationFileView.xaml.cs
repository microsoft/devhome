// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class ConfigurationFileView : UserControl
{
    public ConfigurationFileViewModel ViewModel => (ConfigurationFileViewModel)DataContext;

    public ConfigurationFileView()
    {
        this.InitializeComponent();
    }

    private void SetUpButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // Align the checked state with the enabled state.
        SetUpButton.IsChecked = SetUpButton.IsEnabled;
    }

    private void SetUpButton_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
    {
        // When the toggle button is invoked, its state is changed.
        // Always mark the toggle button as checked to ensure it visually looks toggled ON.
        SetUpButton.IsChecked = true;
    }
}
