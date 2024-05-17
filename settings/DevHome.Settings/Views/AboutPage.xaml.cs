// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Settings.Views;

public sealed partial class AboutPage : AutoFocusPage
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
        try
        {
            var logLocation = Common.Logging.LogFolderRoot ?? string.Empty;
            Process.Start("explorer.exe", $"{logLocation}");
        }
        catch (Exception e)
        {
            var log = Log.ForContext("SourceContext", "AboutPage");
            log.Error(e, $"Error opening log location");
        }
    }
#endif
}
