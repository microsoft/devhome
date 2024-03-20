// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinUIEx;
using static System.Net.Mime.MediaTypeNames;

namespace DevHome.Customization.Views;

public sealed partial class OptimizeDevDriveDialog : ContentDialog
{
    public OptimizeDevDriveDialog()
    {
        this.InitializeComponent();
    }

    private async void OnBrowseButtonClick(object sender, RoutedEventArgs e)
    {
        // Create a folder picker dialog
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop,
            ViewMode = PickerViewMode.List,
        };

        folderPicker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Microsoft.UI.Xaml.Application.Current.GetService<WindowEx>().GetWindowHandle());
        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null)
        {
            DirectoryPathTextBox.Text = folder.Path;
        }
    }

    private void MoveDirectory(string sourceDirectory, string targetDirectory)
    {
        try
        {
            // Create the target directory if it doesn't exist
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // Get all files and subdirectories in the source directory
            var files = Directory.GetFiles(sourceDirectory);
            var subdirectories = Directory.GetDirectories(sourceDirectory);

            // Move files
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var targetFilePath = Path.Combine(targetDirectory, fileName);
                File.Move(file, targetFilePath);
            }

            // Recursively move subdirectories
            foreach (var subdirectory in subdirectories)
            {
                var subdirectoryName = Path.GetFileName(subdirectory);
                var targetSubdirectoryPath = Path.Combine(targetDirectory, subdirectoryName);
                MoveDirectory(subdirectory, targetSubdirectoryPath);
            }

            // Delete the source directory
            Directory.Delete(sourceDirectory, true);
        }
        catch (Exception /*ex*/)
        {
        }
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.User);
        }
        catch (Exception /*ex*/)
        {
        }
    }

    private void OnDirectoryInputConfirmed(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Handle OK button click here
        // string directoryPath = DirectoryPathTextBox.Text;
        // Process the directory path (e.g., validate, save, etc.)
        // ...
        /*
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        // await OptimizeDevDriveDialog.ShowAsync();
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Microsoft.UI.Xaml.Application.Current.GetService<WindowEx>().GetWindowHandle());
        var folder = await folderPicker.PickSingleFolderAsync();
        */

        var directoryPath = DirectoryPathTextBox.Text;

        if (directoryPath != null)
        {
            // Handle the selected folder
            // TODO: If chosen folder not a dev drive location, display the error
            // Else make the changes. TODO: Fix hardcode.
            // MoveDirectory(CacheLocation, folder.Path);
            MoveDirectory("C:\\Users\\sodas\\AppData\\Local\\Packages\\PythonSoftwareFoundation.Python.3.12_qbz5n2kfra8p0\\LocalCache\\Local\\pip\\Cache", directoryPath);
            SetEnvironmentVariable("PIP_CACHE_DIR", directoryPath);
        }
    }

    private void OnDirectoryInputCancelled(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Handle Cancel button click here
        // You can close the dialog or perform any other action
        // ...
    }
}
