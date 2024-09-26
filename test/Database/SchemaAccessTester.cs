// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using DevHome.Database.Services;

namespace DevHome.Test.Database;

internal sealed class SchemaAccessTester : ISchemaAccessor
{
    public uint Version { get; set; }

    private readonly string _schemaPath = Path.Join("SchemaVersionForText.txt");

    public uint GetPreviousSchemaVersion()
    {
        var previousSchemaContents = GetPreviousSchema();
        uint schemaVersion;
        _ = uint.TryParse(previousSchemaContents, out schemaVersion);

        return schemaVersion;
    }

    private string GetPreviousSchema()
    {
        return File.Exists(_schemaPath) ? _schemaPath : string.Empty;
    }

    public void WriteSchemaVersion(uint schemaVersion)
    {
        File.WriteAllText(_schemaPath, schemaVersion.ToString(CultureInfo.InvariantCulture));
    }
}
