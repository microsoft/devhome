// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.FileExplorerSourceControlIntegration.Services;
using FileExplorerSourceControlIntegration;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.Customization.Views;

public sealed partial class FileExplorerPage : DevHomePage
{
    public FileExplorerViewModel ViewModel
    {
        get;
    }

    private string? RootPath
    {
        get; set;
    }

    private static readonly System.Buffers.SearchValues<char> InvalidChars = System.Buffers.SearchValues.Create("<>*?|.");

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
        RootPath = RootPathTextBox.Text;

        if (ValidateRepositoryPath(RootPath))
        {
            // TODO: Determine if the extension GUID should be stored instead of name as it is more identifiable and/or determine any other unique property to use for
            // mapping here
            ViewModel.AddRepositoryPath("git", RootPath);
        }

        ViewModel.RefreshTrackedRepositories();
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
}
