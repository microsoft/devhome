// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.Settings.Views;

public sealed partial class PreferencesPage : Page
{
    public PreferencesViewModel ViewModel { get; }

    public PreferencesPage()
    {
        ViewModel = Application.Current.GetService<PreferencesViewModel>();
        this.InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
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

        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(ParentContainer);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
