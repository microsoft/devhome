// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.Services;

/// <summary>
/// Use to access the database schema file.
/// </summary>
public interface ISchemaAccessor
{
    /// <summary>
    /// Get the schema version stored in a file.
    /// </summary>
    /// <returns>A uint representing the schema version.</returns>
    uint GetPreviousSchemaVersion();

    /// <summary>
    /// Writes schemaVersion to the schema file.
    /// </summary>
    /// <param name="schemaVersion">The new schema version of the database.</param>
    void WriteSchemaVersion(uint schemaVersion);
}
