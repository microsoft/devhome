// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Serilog;

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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus on the first focusable element inside the shell content
        var element = FocusManager.FindFirstFocusableElement(ParentContainer);
        if (element != null)
        {
            await FocusManager.TryFocusAsync(element, FocusState.Programmatic).AsTask();
        }
    }
}
