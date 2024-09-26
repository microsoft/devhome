// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Windows.Storage;

namespace DevHome.Database.Services;

public sealed class MSIXSchemaAccessor : ISchemaAccessor
{
    private readonly string _installPath = Windows.ApplicationModel.Package.Current.InstalledPath;

    public uint GetPreviousSchemaVersion()
    {
        var schemaFileContents = GetPreviousSchema();
        uint schemaVersion;
        _ = uint.TryParse(schemaFileContents, out schemaVersion);

        return schemaVersion;
    }

    public void WriteSchemaVersion(uint schemaVersion)
    {
        var assetsFolder = StorageFolder.GetFolderFromPathAsync(_installPath).AsTask().Result;
        var schemaVersionFile = assetsFolder.CreateFileAsync(SchemaAccessorConstants.SchemaVersionFileName, CreationCollisionOption.OpenIfExists).AsTask().Result;

        FileIO.WriteTextAsync(schemaVersionFile, schemaVersion.ToString(CultureInfo.InvariantCulture)).AsTask().Wait();
    }

    private string GetPreviousSchema()
    {
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

    private StorageFile? GetPreviousSchemaFile()
    {
        try
        {
            var installPathFolder = StorageFolder.GetFolderFromPathAsync(_installPath).AsTask().Result;
            return installPathFolder.CreateFileAsync(SchemaAccessorConstants.SchemaVersionFileName, CreationCollisionOption.OpenIfExists).AsTask().Result;
        }
        catch
        {
            return null;
        }
    }
}
