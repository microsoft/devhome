// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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

    public bool IsHidden { get; set; }

    public bool HasAConfigurationFile { get; set; }

    public string? ConfigurationFileLocation { get; set; }

    // Use string here.  Uri is causing too many problems during add-migration.
    public string? RepositoryUri { get; set; }

    public DateTime? CreatedUTCDate { get; set; }

    public DateTime? UpdatedUTCDate { get; set; }
}
