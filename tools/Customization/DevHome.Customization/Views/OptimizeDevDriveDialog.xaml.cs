// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Storage.Pickers;
using WinUIEx;

namespace DevHome.Customization.Views;

public sealed partial class OptimizeDevDriveDialog : ContentDialog
{
    public OptimizeDevDriveDialog(string cacheToBeMoved, string existingCacheLocation, string environmentVariableToBeSet)
    {
        this.InitializeComponent();
        CacheToBeMoved = cacheToBeMoved;
        ExistingCacheLocation = existingCacheLocation;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        OptimizeDevDriveDialogDescription = stringResource.GetLocalized("OptimizeDevDriveDialogDescription", ExistingCacheLocation, EnvironmentVariableToBeSet);
        ChooseDirectoryPromptText = stringResource.GetLocalized("ChooseDirectoryPromptText");
        ExampleDevDriveLocation = stringResource.GetLocalized("ExampleDevDriveLocation");
    }

    public string ExampleDevDriveLocation
    {
        get => (string)GetValue(ExampleDevDriveLocationProperty);
        set => SetValue(ExampleDevDriveLocationProperty, value);
    }

    public string ChooseDirectoryPromptText
    {
        get => (string)GetValue(ChooseDirectoryPromptTextProperty);
        set => SetValue(ChooseDirectoryPromptTextProperty, value);
    }

    public string CacheToBeMoved
    {
        get => (string)GetValue(CacheToBeMovedProperty);
        set => SetValue(CacheToBeMovedProperty, value);
    }

    public string ExistingCacheLocation
    {
        get => (string)GetValue(ExistingCacheLocationProperty);
        set => SetValue(ExistingCacheLocationProperty, value);
    }

    public string EnvironmentVariableToBeSet
    {
        get => (string)GetValue(EnvironmentVariableToBeSetProperty);
        set => SetValue(EnvironmentVariableToBeSetProperty, value);
    }

    public string OptimizeDevDriveDialogDescription
    {
        get => (string)GetValue(OptimizeDevDriveDialogDescriptionProperty);
        set => SetValue(OptimizeDevDriveDialogDescriptionProperty, value);
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
        catch (Exception ex)
        {
            Log.Error($"Error in MoveDirectory. Error: {ex}");
        }
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.User);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in SetEnvironmentVariable. Error: {ex}");
        }
    }

    private void OnDirectoryInputConfirmed(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var directoryPath = DirectoryPathTextBox.Text;

        if (directoryPath != null)
        {
            // Handle the selected folder
            // TODO: If chosen folder not a dev drive location, currently we no-op. Instead we should display the error.
            MoveDirectory(ExistingCacheLocation, directoryPath);
            SetEnvironmentVariable(EnvironmentVariableToBeSet, directoryPath);
        }
    }

    private void OnDirectoryInputCancelled(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private static readonly DependencyProperty ExampleDevDriveLocationProperty = DependencyProperty.Register(nameof(ExampleDevDriveLocation), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
    private static readonly DependencyProperty ChooseDirectoryPromptTextProperty = DependencyProperty.Register(nameof(ChooseDirectoryPromptText), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
    private static readonly DependencyProperty CacheToBeMovedProperty = DependencyProperty.Register(nameof(CacheToBeMoved), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
    private static readonly DependencyProperty ExistingCacheLocationProperty = DependencyProperty.Register(nameof(ExistingCacheLocation), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
    private static readonly DependencyProperty EnvironmentVariableToBeSetProperty = DependencyProperty.Register(nameof(EnvironmentVariableToBeSet), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
    private static readonly DependencyProperty OptimizeDevDriveDialogDescriptionProperty = DependencyProperty.Register(nameof(OptimizeDevDriveDialogDescription), typeof(string), typeof(OptimizeDevDriveDialog), new PropertyMetadata(null));
}
