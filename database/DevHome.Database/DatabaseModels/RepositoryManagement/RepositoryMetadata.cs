// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

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
