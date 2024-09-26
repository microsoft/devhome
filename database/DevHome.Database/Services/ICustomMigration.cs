// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.Services;

/// <summary>
/// Class to handle any custom migrations between versions that can not be done via a Sqlite script.
/// </summary>
public interface ICustomMigration
{
    /// <summary>
    /// If the class has custom logic to move data from previousSchemaVersion to currentSchemaVersion
    /// </summary>
    /// <param name="previousSchemaVersion">Schema version of the database.</param>
    /// <param name="currentSchemaVersion">Schema version inside DevHome.</param>
    /// <returns>True if this class has code to run.  Otherwise false.</returns>
    bool CanHandleMigration(uint previousSchemaVersion, uint currentSchemaVersion);

    /// <summary>
    /// Gets the priority.  Priority is used to determine the order classes will run if more than
    /// one class can handle the migration.  Lowest priority executes first.
    /// </summary>
    uint Priority { get; }

    /// <summary>
    /// Method to get and save any data for Execute().
    /// </summary>
    /// <returns>True if execute should be called.  False if execution should be skipped.</returns>
    bool PrepareForMigration();

    /// <summary>
    /// Performs the migration using data saved from Execute.
    /// </summary>
    void Migrate();
}
