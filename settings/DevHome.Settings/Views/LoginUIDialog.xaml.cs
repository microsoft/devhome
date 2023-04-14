// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;
public sealed partial class LoginUIDialog : ContentDialog
{
    public LoginUIDialog(StackPanel pluginAdaptiveCardPanel)
    {
        this.InitializeComponent();
        LoginUIContent.Content = pluginAdaptiveCardPanel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }
}
