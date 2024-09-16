// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class PreferencesPage : Page
{
    public PreferencesViewModel ViewModel { get; }

    public PreferencesPage()
    {
        ViewModel = Application.Current.GetService<PreferencesViewModel>();
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        var selectedTheme = ViewModel.ElementTheme;
        foreach (var item in ThemeSelectionComboBox.Items)
        {
            var comboItem = item as ComboBoxItem;
            if (comboItem?.Tag is ElementTheme tag && tag == selectedTheme)
            {
                ThemeSelectionComboBox.SelectedValue = item;
                break;
            }
        }
    }
}
