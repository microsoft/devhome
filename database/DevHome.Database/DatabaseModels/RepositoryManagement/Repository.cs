// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

/// <summary>
/// This represents the SQLite model EF has in the database.
/// Any change here needs a corresponding migration.
/// Use FluentAPI in the database context to further customize each column.
/// </summary>
[Index(nameof(RepositoryName), nameof(RepositoryClonePath), IsUnique = true)]
public class Repository
{
    public int RepositoryId { get; set; }

    public string? RepositoryName { get; set; }

    public string? RepositoryClonePath { get; set; }

    public DateTime? CreatedUTCDate { get; set; }

    public DateTime? UpdatedUTCDate { get; set; }

    // 1:1 relationship.  Repository is the parent and needs only
    // the object of the dependant.
    public RepositoryMetadata? RepositoryMetadata { get; set; }
}
