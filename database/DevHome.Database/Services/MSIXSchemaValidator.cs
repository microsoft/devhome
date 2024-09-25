// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using Windows.Storage;

namespace DevHome.Database.Services;

internal sealed class MSIXSchemaValidator : ISchemaValidator
{
    public bool DoesPreviousSchemaExist()
    {
        return GetPathToSchemaFile() != null;
    }

    public string GetPreviousSchema()
    {
        if (!DoesPreviousSchemaExist())
        {
            return string.Empty;
        }

        var storageFile = GetPreviousSchemaFile();

        try
        {
            return storageFile == null ? string.Empty : FileIO.ReadTextAsync(storageFile as StorageFile).AsTask().Result;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void WriteSchemaVersion(uint schemaVersion)
    {
        var assetsFolderPath = GetPathToSchemaFile();
        var assetsFolder = StorageFolder.GetFolderFromPathAsync(assetsFolderPath).AsTask().Result;
        var schemaVersionFile = assetsFolder.CreateFileAsync(SchemaHelper.SchemaVersionFileName, CreationCollisionOption.OpenIfExists).AsTask().Result;

        FileIO.WriteTextAsync(schemaVersionFile, schemaVersion.ToString(CultureInfo.InvariantCulture)).AsTask().Wait();
    }

    public uint GetPreviousSchemaVersion(string schemaFileContents)
    {
        uint schemaVersion;
        _ = uint.TryParse(schemaFileContents, out schemaVersion);

        return schemaVersion;
    }

    private IStorageItem? GetPreviousSchemaFile()
    {
        try
        {
            var assetsFolderPath = GetPathToSchemaFile();
            var assetsFolder = StorageFolder.GetFolderFromPathAsync(assetsFolderPath).AsTask().Result;

            return assetsFolder.TryGetItemAsync(SchemaHelper.SchemaVersionFileName).AsTask().Result;
        }
        catch
        {
            return null;
        }
    }

    private string GetPathToSchemaFile()
    {
        var root = Windows.ApplicationModel.Package.Current.InstalledPath;
        return Path.Join(root, SchemaHelper.SchemaVersionDirectoryPath);
    }
}
