// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.IO;

namespace DevHome.Database.Services;

internal class WinExeSchemaValidator : ISchemaValidator
{
    private readonly string _schemaPath = Path.Join(SchemaHelper.SchemaVersionDirectoryPath, SchemaHelper.SchemaVersionFileName);

    public bool DoesPreviousSchemaExist()
    {
        return File.Exists(_schemaPath);
    }

    public string GetPreviousSchema()
    {
        if (!DoesPreviousSchemaExist())
        {
            return string.Empty;
        }

        return File.ReadAllText(_schemaPath);
    }

    public uint GetPreviousSchemaVersion(string schemaFileContents)
    {
        uint schemaVersion;
        _ = uint.TryParse(schemaFileContents, out schemaVersion);

        return schemaVersion;
    }

    public void WriteSchemaVersion(uint schemaVersion)
    {
        File.WriteAllText(_schemaPath, schemaVersion.ToString(CultureInfo.InvariantCulture));
    }
}
