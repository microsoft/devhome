// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Logging;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class AboutPage : Page
{
    public AboutViewModel ViewModel { get; }

    public AboutPage()
    {
        ViewModel = Application.Current.GetService<AboutViewModel>();
        this.InitializeComponent();

#if DEBUG
        Loaded += ShowViewLogsButton;
#endif
    }

#if DEBUG
    private void ShowViewLogsButton(object sender, RoutedEventArgs e)
    {
        ViewLogsSettingsCard.Visibility = Visibility.Visible;
        ViewLogsSettingsCard.Command = OpenLogsLocationCommand;
    }

    [RelayCommand]
    private void OpenLogsLocation()
    {
        var logLocation = GlobalLog.Logger?.Options.LogFileFolderRoot ?? string.Empty;
        try
        {
            Process.Start("explorer.exe", $"{logLocation}");
        }
        catch (Exception e)
        {
            GlobalLog.Logger?.ReportError("AboutPage", $"Error opening log location", e);
        }
    }
#endif
}
