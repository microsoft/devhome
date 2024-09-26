// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.IO;

namespace DevHome.Database.Services;

public class WinExeSchemaAccessor : ISchemaAccessor
{
    private readonly string _schemaPath = Path.Join(SchemaAccessorConstants.SchemaVersionFileName);

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
