// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using DevHome.Common.Extensions;
using DevHome.PI.Helpers;
using DevHome.PI.Models;
using DevHome.PI.Properties;
using DevHome.PI.ToolWindows;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace DevHome.PI.SettingsUi;

public sealed partial class SettingsToolWindow : ToolWindow
{
    private readonly string userProfileTabName = CommonHelper.GetLocalizedString("UserProfileTabName");
    private readonly string additionalToolsTabName = CommonHelper.GetLocalizedString("AdditionalToolsTabName");
    private readonly string advancedSettingsTabName = CommonHelper.GetLocalizedString("AdvancedSettingsTabName");
    private readonly string aboutTabName = CommonHelper.GetLocalizedString("AboutTabName");

    public List<NavLink> Links { get; set; } = [];

    public SettingsToolWindow(StringCollection pos)
        : base(pos)
    {
        Links.Add(new NavLink("\uE716", userProfileTabName, null));
        Links.Add(new NavLink("\uEC7A", additionalToolsTabName, null));
        Links.Add(new NavLink("\uE90F", advancedSettingsTabName, null));
        Links.Add(new NavLink("\uE946", aboutTabName, null));

        InitializeComponent();

        NavLinksList.ItemsSource = Links;
        NavLinksList.SelectedIndex = 0;

        CpuUsageToggle.IsOn = Settings.Default.IsCpuUsageMonitoringEnabled;
        ShowInsightsToggle.IsOn = Settings.Default.IsInsightsOnStartupEnabled;
        EnableClipboardMonitoringToggle.IsOn = Settings.Default.IsClipboardMonitoringEnabled;
        var item = ThemeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(
            item => string.Equals(item.Content.ToString(), Settings.Default.CurrentTheme, StringComparison.OrdinalIgnoreCase));
        ThemeComboBox.SelectedItem = item;
        ThemeName t = ThemeName.Themes.First(t => t.Name == Settings.Default.CurrentTheme);
        SetRequestedTheme(t.Theme);
        SetTitleBarColors();
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

        var excludedProcesses = Settings.Default.ExcludedProcesses;
        StringBuilder builder = new();
        foreach (var process in excludedProcesses)
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            builder.Append($"{process}, ");
#pragma warning restore CA1305 // Specify IFormatProvider
        }

        ExcludedProcessesTextBox.Text = builder.ToString();
    }

    private void SetRequestedTheme(ElementTheme theme)
    {
        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    // TODO Call this when the theme changes also.
    private bool SetTitleBarColors()
    {
        // Check to see if customization is supported.
        // The method returns true on Windows 10 since Windows App SDK 1.2,
        // and on all versions of Windows App SDK on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;

            if (Content is FrameworkElement rootElement)
            {
                Color solidBackgroundFillColorBase;
                Color textFillColorPrimary;
                Color controlSolidFillColorDefault;
                Color controlFillColorInputActive;
                if (rootElement.RequestedTheme == ElementTheme.Dark)
                {
                    // From WinUiThemeResources.xaml Default theme.
                    // <Color x:Key="TextFillColorPrimary">#FFFFFF</Color>
                    // <Color x:Key="SolidBackgroundFillColorBase">#202020</Color>
                    // <Color x:Key="ControlSolidFillColorDefault">#454545</Color>
                    // <Color x:Key="ControlFillColorInputActive">#B31E1E1E</Color>
                    textFillColorPrimary = Color.FromArgb(255, 255, 255, 255);
                    solidBackgroundFillColorBase = Color.FromArgb(255, 32, 32, 32);
                    controlSolidFillColorDefault = Color.FromArgb(255, 69, 69, 69);
                    controlFillColorInputActive = Color.FromArgb(179, 30, 30, 30);
                }
                else
                {
                    // From WinUiThemeResources.xaml Light theme.
                    // <Color x:Key="TextFillColorPrimary">#E4000000</Color>
                    // <Color x:Key="SolidBackgroundFillColorBase">#F3F3F3</Color>
                    // <Color x:Key="ControlSolidFillColorDefault">#FFFFFF</Color>
                    // <Color x:Key="ControlFillColorInputActive">#FFFFFF</Color>
                    textFillColorPrimary = Color.FromArgb(228, 0, 0, 0);
                    solidBackgroundFillColorBase = Color.FromArgb(255, 243, 243, 243);
                    controlSolidFillColorDefault = Color.FromArgb(255, 255, 255, 255);
                    controlFillColorInputActive = Color.FromArgb(255, 255, 255, 255);
                }

                titleBar.ForegroundColor = textFillColorPrimary;
                titleBar.BackgroundColor = solidBackgroundFillColorBase;
                titleBar.ButtonBackgroundColor = controlSolidFillColorDefault;
                titleBar.ButtonHoverBackgroundColor = controlSolidFillColorDefault;
                titleBar.ButtonPressedBackgroundColor = controlFillColorInputActive;
            }

            return true;
        }

        return false;
    }

    private void NavLinksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavLinksList.SelectedIndex > -1)
        {
            if (NavLinksList.SelectedItem is NavLink link)
            {
                UserProfileGrid.Visibility = Visibility.Collapsed;
                AdditionalToolsGrid.Visibility = Visibility.Collapsed;
                AdvancedSettingsGrid.Visibility = Visibility.Collapsed;
                AboutGrid.Visibility = Visibility.Collapsed;
                if (link.ContentText == userProfileTabName)
                {
                    UserProfileGrid.Visibility = Visibility.Visible;
                }
                else if (link.ContentText == additionalToolsTabName)
                {
                    AdditionalToolsGrid.Visibility = Visibility.Visible;
                }
                else if (link.ContentText == advancedSettingsTabName)
                {
                    AdvancedSettingsGrid.Visibility = Visibility.Visible;
                }
                else if (link.ContentText == aboutTabName)
                {
                    AboutGrid.Visibility = Visibility.Visible;
                }
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedIndex > -1)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                var newTheme = (string)item.Content;
                ThemeName t = ThemeName.Themes.First(t => t.Name == newTheme);
                SetRequestedTheme(t.Theme);

                var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
                barWindow?.SetRequestedTheme(t.Theme);
                SetTitleBarColors();
                Settings.Default.CurrentTheme = newTheme;
                Settings.Default.Save();
            }
        }
    }

    private void CpuUsageToggle_Toggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.IsCpuUsageMonitoringEnabled = CpuUsageToggle.IsOn;
        Settings.Default.Save();

        if (CpuUsageToggle.IsOn)
        {
            PerfCounters.Instance.Start();
        }
        else
        {
            PerfCounters.Instance.Stop();
        }
    }

    private void ShowInsightsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.IsInsightsOnStartupEnabled = ShowInsightsToggle.IsOn;
        Settings.Default.Save();
    }

    private void EnableClipboardMonitoringToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (Settings.Default.IsClipboardMonitoringEnabled != EnableClipboardMonitoringToggle.IsOn)
        {
            Settings.Default.IsClipboardMonitoringEnabled = EnableClipboardMonitoringToggle.IsOn;
            Settings.Default.Save();
        }
    }

    private void ExcludedProcessesButton_Click(object sender, RoutedEventArgs e)
    {
        var excludedProcesses = ExcludedProcessesTextBox.Text;
        if (!string.IsNullOrEmpty(excludedProcesses))
        {
            Settings.Default.ExcludedProcesses.Clear();
            var processes = excludedProcesses.Split(',');
            foreach (var process in processes)
            {
                Settings.Default.ExcludedProcesses.Add(process.Trim());
            }

            Settings.Default.Save();
        }
    }
}
