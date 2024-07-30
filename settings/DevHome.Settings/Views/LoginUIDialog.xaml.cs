// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace DevHome.Settings.Views;

public sealed partial class LoginUIDialog : ContentDialog
{
    public LoginUIDialog(StackPanel extensionAdaptiveCardPanel)
    {
        this.InitializeComponent();
        LoginUIContent.Content = extensionAdaptiveCardPanel;
        RequestedTheme = Application.Current.GetService<IThemeSelectorService>().Theme;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    // drop UI code here?
}
