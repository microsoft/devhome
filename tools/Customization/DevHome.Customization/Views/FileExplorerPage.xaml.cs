// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Customization.Views;

public sealed partial class FileExplorerPage : Page
{
    public FileExplorerViewModel ViewModel
    {
        get;
    }

    private string? RootPath
    {
        get; set;
    }

    private static readonly System.Buffers.SearchValues<char> InvalidChars = System.Buffers.SearchValues.Create("<>*?|");

    private readonly Serilog.ILogger log = Log.ForContext("SourceContext", nameof(FileExplorerPage));

    public FileExplorerPage()
    {
        ViewModel = Application.Current.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
        var experimentationService = Application.Current.GetService<IExperimentationService>();
        if (experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration"))
        {
            TrackRepository.Visibility = Visibility.Visible;
            DisplayTrackRepository.Visibility = Visibility.Visible;
        }
    }

    public void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var numProviders = ViewModel.LocalRepositoryProviders.Count;
        if (numProviders == 1)
        {
            RegisterRepository(ViewModel.LocalRepositoryProviders[0].LocalRepositoryProvider, RootPathTextBox.Text);
        }
        else if (numProviders > 1)
        {
            LocalRepositoryProvidersFlyout.ShowAt(sender as Button);
        }
    }

    public bool ValidateRepositoryPath(string rootPath)
    {
        if (!Path.IsPathFullyQualified(rootPath))
        {
            log.Warning("Path is not fully qualified or maybe a UNC path.");
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        if (rootPath.IndexOfAny(Path.GetInvalidPathChars()) != -1 ||
            rootPath.AsSpan().IndexOfAny(InvalidChars) != -1)
        {
            log.Warning("Path contains invalid chars");
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        if (!Array.Exists(Environment.GetLogicalDrives(), d => d.Equals(Path.GetPathRoot(rootPath), StringComparison.OrdinalIgnoreCase)))
        {
            log.Warning("Drive provided does not exist on users machine");
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        return true;
    }

    private void AddRepository_Click(object sender, RoutedEventArgs e)
    {
        if (sender as Button is Button registerRepoButton)
        {
            if (registerRepoButton.Tag is FileExplorerSourceControlIntegrationViewModel fileExplorerSourceControlIntegrationViewModel)
            {
                RegisterRepository(fileExplorerSourceControlIntegrationViewModel.LocalRepositoryProvider, RootPathTextBox.Text);
            }
            else
            {
                log.Information($"AddRepository_Click(): registerRepoButton.Tag is not FileExplorerSourceControlIntegrationViewModel - Sender: {sender} RoutedEventArgs: {e}");
                return;
            }
        }
    }

    public void RegisterRepository(IExtensionWrapper localRepositoryProvider, string rootPath)
    {
        if (ValidateRepositoryPath(rootPath))
        {
            if (!ViewModel.AddRepositoryPath(localRepositoryProvider.ExtensionClassId, RootPathTextBox.Text))
            {
                RootPathErrorBar.IsOpen = true;
            }
        }

        ViewModel.RefreshTrackedRepositories();
    }
}
