// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class PreferencesPage : AutoFocusPage
{
    public PreferencesViewModel ViewModel { get; }

    public PreferencesPage()
    {
        ViewModel = Application.Current.GetService<PreferencesViewModel>();
        this.InitializeComponent();
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
