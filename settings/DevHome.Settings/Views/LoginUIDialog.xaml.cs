// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class LoginUIDialog : ContentDialog
{
    public LoginUIDialog(StackPanel extensionAdaptiveCardPanel)
    {
        this.InitializeComponent();
        LoginUIContent.Content = extensionAdaptiveCardPanel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }
}
