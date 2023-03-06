// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.RepoConfig.Models;
using DevHome.SetupFlow.RepoConfig.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.RepoConfig.Views;

public sealed partial class RepoConfigView : UserControl
{
    public RepoConfigView()
    {
        this.InitializeComponent();
    }

    private readonly CloningInformation _cloningInformation = new ();

    public RepoConfigViewModel ViewModel => (RepoConfigViewModel)this.DataContext;

    private async void AddRepoButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var addRepoDialog = new AddRepoDialog(_cloningInformation);
        await addRepoDialog.StartPlugins();
        var themeService = Application.Current.GetService<IThemeSelectorService>();
        addRepoDialog.XamlRoot = RepoConfigRelativePanel.XamlRoot;
        addRepoDialog.RequestedTheme = themeService.Theme;
        var result = await addRepoDialog.ShowAsync(ContentDialogPlacement.InPlace);

        if (result == ContentDialogResult.Primary)
        {
            ViewModel.SaveRepoInformation(_cloningInformation);
        }
    }
}
