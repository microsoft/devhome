// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

/// <summary>
/// This represents the SQLite model EF has in the database.
/// Any change here needs a corresponding migration.
/// Use FluentAPI in the database context to further customize each column.
/// </summary>
public class RepositoryMetadata
{
    // EF uses [ClassName]Id as the primary key
    public int RepositoryMetadataId { get; set; }

    public bool IsHiddenFromPage { get; set; }

    public DateTime UtcDateHidden { get; set; }

    public DateTime? CreatedUTCDate { get; set; }

    public DateTime? UpdatedUTCDate { get; set; }

    public int RepositoryId { get; set; }

    public Repository Repository { get; set; } = null!;
}
