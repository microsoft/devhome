// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

[Index(nameof(RepositoryName), nameof(RepositoryClonePath), IsUnique = true)]
public class Repository
{
    public int RepositoryId { get; set; }

    public string? RepositoryName { get; set; }

    public string? RepositoryClonePath { get; set; }

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

        if (objAsRepository.RepositoryName.Equals(RepositoryName, StringComparison.OrdinalIgnoreCase)
            || objAsRepository.RepositoryClonePath.Equals(RepositoryClonePath, StringComparison.OrdinalIgnoreCase))
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
