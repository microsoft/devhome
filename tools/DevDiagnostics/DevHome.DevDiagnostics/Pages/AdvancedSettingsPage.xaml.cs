// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Properties;
using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevDiagnostics.Pages;

public sealed partial class AdvancedSettingsPage : Page
{
    public AdvancedSettingsViewModel ViewModel { get; }

    public AdvancedSettingsPage()
    {
        ViewModel = Application.Current.GetService<AdvancedSettingsViewModel>();
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        CpuUsageToggle.IsOn = Settings.Default.IsCpuUsageMonitoringEnabled;
        ShowInsightsToggle.IsOn = Settings.Default.IsInsightsOnStartupEnabled;
        EnableClipboardMonitoringToggle.IsOn = Settings.Default.IsClipboardMonitoringEnabled;

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

    private void CpuUsageToggle_Toggled(object sender, RoutedEventArgs e)
    {
        Settings.Default.IsCpuUsageMonitoringEnabled = CpuUsageToggle.IsOn;
        Settings.Default.Save();
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
