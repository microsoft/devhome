// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Logging;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class AboutPage : Page
{
    public AboutViewModel ViewModel { get; }

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AboutPage()
    {
        ViewModel = Application.Current.GetService<AboutViewModel>();
        this.InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_About_Header"), typeof(AboutViewModel).FullName!),
        };

#if DEBUG
        Loaded += ShowViewLogsButton;
#endif
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
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
