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

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj is not Repository)
        {
            return false;
        }

        var objAsRepository = obj as Repository;
        if (objAsRepository!.RepositoryName == null ||
            objAsRepository.RepositoryClonePath == null)
        {
            return false;
        }

        // Compare names.  Comparing paths will add too much code in one place.
        if (!objAsRepository.RepositoryName.Equals(RepositoryName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var leftPath = Path.GetFullPath(objAsRepository.RepositoryClonePath);

        // Made sure RepositoryClonePath is not null earlier.
        var rightPath = Path.GetFullPath(RepositoryClonePath!);

        // DevHome is on windows.  If not on windows, this will change.
        if (!string.Equals(leftPath, rightPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return (RepositoryName, RepositoryClonePath, RepositoryMetadata).GetHashCode();
    }
}
