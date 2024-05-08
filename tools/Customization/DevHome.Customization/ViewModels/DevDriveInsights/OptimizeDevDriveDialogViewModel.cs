// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Media.Protection;
using Windows.Storage.Pickers;
using WinUIEx;
using YamlDotNet.Core.Tokens;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace DevHome.Customization.ViewModels.DevDriveInsights;

/// <summary>
/// View model for the card that represents a dev drive on the dev drive insights page.
/// </summary>
public partial class OptimizeDevDriveDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private List<string> _existingDevDriveLetters;

    [ObservableProperty]
    private string _exampleDevDriveLocation;

    [ObservableProperty]
    private string _chooseDirectoryPromptText;

    [ObservableProperty]
    private string _makeChangesText;

    [ObservableProperty]
    private string _existingCacheLocation;

    [ObservableProperty]
    private string _environmentVariableToBeSet;

    [ObservableProperty]
    private string _optimizeDevDriveDialogDescription;

    [ObservableProperty]
    private string _directoryPathTextBox;

    public OptimizeDevDriveDialogViewModel(
        string existingCacheLocation,
        string environmentVariableToBeSet,
        string exampleDevDriveLocation,
        List<string> existingDevDriveLetters)
    {
        DirectoryPathTextBox = string.Empty;
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        ExistingDevDriveLetters = existingDevDriveLetters;
        ExampleDevDriveLocation = stringResource.GetLocalized("ExampleText") + exampleDevDriveLocation;
        ChooseDirectoryPromptText = stringResource.GetLocalized("ChooseDirectoryPromptText");
        MakeChangesText = stringResource.GetLocalized("MakeChangesText");
        ExistingCacheLocation = existingCacheLocation;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
        OptimizeDevDriveDialogDescription = stringResource.GetLocalized("OptimizeDevDriveDialogDescription/Text", ExistingCacheLocation, EnvironmentVariableToBeSet);
    }

    [RelayCommand]
    private void DirectoryPathChanged(string text)
    {
        DirectoryPathTextBox = text;
    }

    [RelayCommand]
    private async Task BrowseButtonClick(object sender)
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
            DirectoryPathTextBox = folder.Path;
        }
    }

    private string RemovePrivacyInfo(string input)
    {
        var output = input;
        var userProfilePath = Environment.ExpandEnvironmentVariables("%userprofile%");
        if (input.StartsWith(userProfilePath, StringComparison.OrdinalIgnoreCase))
        {
            var index = input.LastIndexOf(userProfilePath, StringComparison.OrdinalIgnoreCase) + userProfilePath.Length;
            output = Path.Join("%userprofile%", input.Substring(index));
        }

        return output;
    }

    private bool MoveDirectory(string sourceDirectory, string targetDirectory)
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
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"Error in MoveDirectory. Error: {ex}");
            TelemetryFactory.Get<ITelemetry>().LogError("DevDriveInsights_PackageCacheMoveDirectory_Error", LogLevel.Critical, new ExceptionEvent(ex.HResult, RemovePrivacyInfo(sourceDirectory)));
            return false;
        }
    }

    private void UpdatePathEnvironmentVariable(string value)
    {
        var pathEnvironmentVariable = "PATH";
        var existingValue = Environment.GetEnvironmentVariable(pathEnvironmentVariable, EnvironmentVariableTarget.User);
        if (existingValue != null)
        {
            // Split the existing value into parts
            var parts = existingValue.Split(';');

            // Check if the specific value exists
            var valueExists = false;
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim().Equals(ExistingCacheLocation, StringComparison.OrdinalIgnoreCase))
                {
                    // Replace the existing value
                    parts[i] = value;
                    valueExists = true;
                    break;
                }
            }

            if (!valueExists)
            {
                // Add the new value
                Array.Resize(ref parts, parts.Length + 1);
                parts[parts.Length - 1] = Path.Join(value, "bin");
            }

            // Join the modified parts back together
            var modifiedValue = string.Join(";", parts);

            // Set the modified value to the environment variable
            Environment.SetEnvironmentVariable(pathEnvironmentVariable, modifiedValue, EnvironmentVariableTarget.User);
        }
        else
        {
            // The environment variable doesn't exist, add the new value
            Environment.SetEnvironmentVariable(pathEnvironmentVariable, value, EnvironmentVariableTarget.User);
        }
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.User);

            if (string.Equals(variableName, "CARGO_HOME", StringComparison.OrdinalIgnoreCase))
            {
                // Check if PATH environment variable contains existing cargo location.
                // If so, update that to new location. Otherwise append the new location.
                UpdatePathEnvironmentVariable(value);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error in SetEnvironmentVariable. Error: {ex}");
        }
    }

    private bool ChosenDirectoryInDevDrive(string directoryPath)
    {
        foreach (var devDriveLetter in ExistingDevDriveLetters)
        {
            if (directoryPath.StartsWith(devDriveLetter + ":", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private void DirectoryInputConfirmed()
    {
        var directoryPath = DirectoryPathTextBox;

        if (!string.IsNullOrEmpty(directoryPath))
        {
            // Handle the selected folder
            // TODO: If chosen folder not a dev drive location, currently we no-op and log the error. Instead we should display the error.
            if (ChosenDirectoryInDevDrive(directoryPath))
            {
                if (MoveDirectory(ExistingCacheLocation, directoryPath))
                {
                    SetEnvironmentVariable(EnvironmentVariableToBeSet, directoryPath);
                    var existingCacheLocationVetted = RemovePrivacyInfo(ExistingCacheLocation);
                    Log.Debug($"Moved cache from {existingCacheLocationVetted} to {directoryPath}");
                    TelemetryFactory.Get<ITelemetry>().Log("DevDriveInsights_PackageCacheMovedSuccessfully_Event", LogLevel.Critical, new ExceptionEvent(0, existingCacheLocationVetted));
                }
            }
            else
            {
                Log.Error($"Chosen directory {directoryPath} not on a dev drive.");
            }
        }
    }
}
