// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.Common.Environments.CustomControls;

public sealed partial class DevDriveOptimizerCardBody : UserControl
{
    public const string DefaultDevDriveOptimizerCardBodyImagePath = "ms-appx:///DevHome.Common/Environments/Assets/devDrive-64.png";

    public DevDriveOptimizerCardBody()
    {
        this.InitializeComponent();
    }

    public DataTemplate ActionControlTemplate
    {
        get => (DataTemplate)GetValue(ActionControlTemplateProperty);
        set => SetValue(ActionControlTemplateProperty, value);
    }

    public string CacheToBeMoved
    {
        get => (string)GetValue(CacheToBeMovedProperty);
        set => SetValue(CacheToBeMovedProperty, value);
    }

    public string CacheLocation
    {
        get => (string)GetValue(CacheLocationProperty);
        set => SetValue(CacheLocationProperty, value);
    }

    public string OptimizationDescription
    {
        get => (string)GetValue(OptimizationDescriptionProperty);
        set => SetValue(OptimizationDescriptionProperty, value);
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

    private async void OpenPopup_Click(object sender, RoutedEventArgs e)
    {
        // popup.IsOpen = true; // Open the popup
        var dialog = new ContentDialog
        {
            Title = "Choose directory on dev drive...",
            Content = "Contents of Pip/Cache will be copied to chosen directory. And PIP_CACHE_DIR will be set to chosen directory.",
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
            XamlRoot = this.Content.XamlRoot,
        };

        await dialog.ShowAsync();

        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Application.Current.GetService<WindowEx>().GetWindowHandle());
        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            // Handle the selected folder
            // TODO: If chosen folder not a dev drive location, display the error
            // Else make the changes. TODO: Fix hardcode.
            MoveDirectory(CacheLocation, folder.Path);
            SetEnvironmentVariable("PIP_CACHE_DIR", folder.Path);
        }

        // TODO: Display that the cache location is optimized now
    }

    private static readonly DependencyProperty ActionControlTemplateProperty = DependencyProperty.Register(nameof(ActionControlTemplate), typeof(DataTemplate), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheToBeMovedProperty = DependencyProperty.Register(nameof(CacheToBeMoved), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheLocationProperty = DependencyProperty.Register(nameof(CacheLocation), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizationDescriptionProperty = DependencyProperty.Register(nameof(OptimizationDescription), typeof(string), typeof(DevDriveOptimizerCardBody), new PropertyMetadata(null));
}
