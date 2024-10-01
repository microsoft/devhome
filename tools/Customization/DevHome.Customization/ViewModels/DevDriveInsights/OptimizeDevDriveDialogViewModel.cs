// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Customization.Models;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage.Pickers;

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

    [ObservableProperty]
    private bool _isPrimaryButtonEnabled;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _isNotDevDrive;

    private List<string> RelatedEnvironmentVariablesToBeSet { get; set; } = new List<string>();

    private List<string> RelatedCacheDirectories { get; set; } = new List<string>();

    public OptimizeDevDriveDialogViewModel(
        string existingCacheLocation,
        string environmentVariableToBeSet,
        string exampleDevDriveLocation,
        List<string> existingDevDriveLetters,
        List<string> relatedEnvironmentVariablesToBeSet,
        List<string> relatedCacheDirectories)
    {
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        ExistingDevDriveLetters = existingDevDriveLetters;
        ExampleDevDriveLocation = stringResource.GetLocalized("ExampleText") + exampleDevDriveLocation;
        DirectoryPathTextBox = exampleDevDriveLocation;
        ChooseDirectoryPromptText = stringResource.GetLocalized("ChooseDirectoryPromptText");
        MakeChangesText = stringResource.GetLocalized("MakeChangesText");
        ExistingCacheLocation = existingCacheLocation;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
        OptimizeDevDriveDialogDescription = stringResource.GetLocalized("OptimizeDevDriveDialogDescription/Text", ExistingCacheLocation, EnvironmentVariableToBeSet);
        IsPrimaryButtonEnabled = true;
        ErrorMessage = string.Empty;
        IsNotDevDrive = false;
        RelatedEnvironmentVariablesToBeSet = relatedEnvironmentVariablesToBeSet;
        RelatedCacheDirectories = relatedCacheDirectories;
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
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, Application.Current.GetService<Window>().GetWindowHandle());
        var folder = await folderPicker.PickSingleFolderAsync();

        if (folder != null)
        {
            DirectoryPathTextBox = folder.Path;
            if (ChosenDirectoryInDevDrive(DirectoryPathTextBox))
            {
                IsPrimaryButtonEnabled = true;
                IsNotDevDrive = false;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
                IsNotDevDrive = true;
                var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
                ErrorMessage = stringResource.GetLocalized("ChosenDirectoryNotOnDevDriveErrorText");
            }
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

    private void UpdateOrCreateSubkey(string xmlFilePath, string key, string subkey, string newValue)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(xmlFilePath);

        XmlNode? keyNode = doc.SelectSingleNode($"{key}");
        if (keyNode != null)
        {
            XmlNode? subkeyNode = keyNode.SelectSingleNode(subkey);
            if (subkeyNode != null)
            {
                // Update the subkey value
                subkeyNode.InnerText = newValue;
            }
            else
            {
                // Create the subkey with the new value
                XmlElement newSubkey = doc.CreateElement(subkey);
                newSubkey.InnerText = newValue;
                keyNode.AppendChild(newSubkey);
            }
        }
        else
        {
            // Create the key and subkey with the new value
            XmlElement newKey = doc.CreateElement(key);
            XmlElement newSubkey = doc.CreateElement(subkey);
            newSubkey.InnerText = newValue;
            newKey.AppendChild(newSubkey);
            if (doc.DocumentElement != null)
            {
                doc.DocumentElement.AppendChild(newKey);
            }
        }

        doc.Save(xmlFilePath);
    }

    private void UpdateSettingsXML(string localRepositoryLocation)
    {
        int index = ExistingCacheLocation.IndexOf("repository", StringComparison.OrdinalIgnoreCase);
        var settingsXMLPath = string.Concat(ExistingCacheLocation.AsSpan(0, index), "settings.xml");
        var key = "settings";
        var subkey = "localRepository";

        if (File.Exists(settingsXMLPath))
        {
            // Update settings.xml with the new repository path
            UpdateOrCreateSubkey(settingsXMLPath, key, subkey, localRepositoryLocation);
        }
        else
        {
            // Create the root element
            XElement root = new XElement(key);

            // Add child element
            root.Add(new XElement(subkey, localRepositoryLocation));

            // Save the XML document to the specified file path
            XDocument doc = new XDocument(root);
            if (index >= 0)
            {
                doc.Save(settingsXMLPath);
            }
        }
    }

    private void SetEnvironmentVariable(string variableName, string value)
    {
        try
        {
            if (string.Equals(variableName, "MAVEN_OPTS", StringComparison.OrdinalIgnoreCase))
            {
                var existingValue = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
                var newValue = (!string.IsNullOrEmpty(existingValue) ? existingValue + " " : string.Empty) + "-Dmaven.repo.local = " + value;
                Environment.SetEnvironmentVariable(variableName, newValue, EnvironmentVariableTarget.User);
                UpdateSettingsXML(value);
                return;
            }

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

    private bool MoveDirectories(string sourceDirectory, string targetDirectory, List<string> relatedCacheDirectories)
    {
        // TODO: If in future we support some cache with multiple relatedCacheDirectories, we should consider using Parallel.ForEachAsync
        foreach (var relatedCacheDirectory in relatedCacheDirectories)
        {
            var relatedCacheDirectoryName = Path.GetFileName(relatedCacheDirectory);
            if (!MoveDirectory(relatedCacheDirectory, $@"{targetDirectory}\Related Directories\{relatedCacheDirectoryName}"))
            {
                return false;
            }
        }

        return MoveDirectory(sourceDirectory, targetDirectory);
    }

    private void SetRelatedEnvironmentVariables(List<string> relatedEnvironmentVariablesToBeSet, List<string> relatedCacheDirectories, string directoryPath)
    {
        var index = 0;
        foreach (var relatedEnvironmentVariableToBeSet in relatedEnvironmentVariablesToBeSet)
        {
            var relatedCacheDirectoryName = Path.GetFileName(relatedCacheDirectories[index]);
            SetEnvironmentVariable(relatedEnvironmentVariableToBeSet, $@"{directoryPath}\Related Directories\{relatedCacheDirectoryName}");
            index++;
        }
    }

    [RelayCommand]
    private void DirectoryInputConfirmed()
    {
        var directoryPath = DirectoryPathTextBox;

        if (!string.IsNullOrEmpty(directoryPath))
        {
            if (ChosenDirectoryInDevDrive(directoryPath))
            {
                Task.Run(() =>
                {
                    if (MoveDirectories(ExistingCacheLocation, directoryPath, RelatedCacheDirectories))
                    {
                        SetRelatedEnvironmentVariables(RelatedEnvironmentVariablesToBeSet, RelatedCacheDirectories, directoryPath);
                        SetEnvironmentVariable(EnvironmentVariableToBeSet, directoryPath);
                        var existingCacheLocationVetted = RemovePrivacyInfo(ExistingCacheLocation);
                        Log.Debug($"Moved cache from {existingCacheLocationVetted} to {directoryPath}");
                        TelemetryFactory.Get<ITelemetry>().Log(
                            "DevDriveInsights_PackageCacheMovedSuccessfully_Event",
                            LogLevel.Critical,
                            new ExceptionEvent(0, existingCacheLocationVetted));

                        // Send message to the DevDriveInsightsViewModel to let it refresh the Dev Drive insights UX
                        WeakReferenceMessenger.Default.Send(new DevDriveOptimizedMessage(new DevDriveOptimizedData()));
                    }
                });
            }
            else
            {
                Log.Error($"Chosen directory {directoryPath} not on a dev drive.");
            }
        }
    }
}
