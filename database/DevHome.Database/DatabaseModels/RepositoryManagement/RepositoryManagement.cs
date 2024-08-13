// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.Database.DatabaseModels.RepositoryManagement;

public class RepositoryManagement
{
    public int RepositoryManagementId { get; set; }

    public bool IsHiddenFromPage { get; set; }

    public DateTime UtcDateHidden { get; set; }

    public int RepositoryId { get; set; }

    public Repository Repository { get; set; } = null!;
}
