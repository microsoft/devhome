// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.FileExplorerSourceControlIntegration.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    private static readonly System.Buffers.SearchValues<char> InvalidChars = System.Buffers.SearchValues.Create("<>*?|.");

    public FileExplorerPage()
    {
        ViewModel = Application.Current.GetService<FileExplorerViewModel>();
        this.InitializeComponent();
        var experimentationService = Application.Current.GetService<IExperimentationService>();
        var isEnabled = experimentationService.IsFeatureEnabled("FileExplorerSourceControlIntegration");
        if (isEnabled)
        {
            TrackRepository.Visibility = Visibility.Visible;
            DisplayTrackRepository.Visibility = Visibility.Visible;
        }
    }

    public void AddButton_Click(object sender, RoutedEventArgs e)
    {
        RootPath = RootPathTextBox.Text;

        var isValidated = ValidateRepositoryPath(RootPath);
        if (isValidated)
        {
            // TO DO: Determine if the extension GUID should be stored instead of name as it is more identifiable and/or determine any other unique property to use for
            // mapping here
            ViewModel.AddRepositoryPath("git", RootPath);
        }

        ViewModel.RefreshTrackedRepositories();
    }

    public bool ValidateRepositoryPath(string rootPath)
    {
        if (!Path.IsPathFullyQualified(rootPath))
        {
            // Error: Path is not fully qualified or maybe a UNC path.
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        if (rootPath.IndexOfAny(Path.GetInvalidPathChars()) != -1 ||
            rootPath.AsSpan().IndexOfAny(InvalidChars) != -1)
        {
            // Error: Path contains invalid chars
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        if (!Array.Exists(Environment.GetLogicalDrives(), d => d.Equals(Path.GetPathRoot(rootPath), StringComparison.OrdinalIgnoreCase)))
        {
            // Error: Drive provided does not exist on users machine
            RootPathErrorBar.IsOpen = true;
            return false;
        }

        return true;
    }
}
