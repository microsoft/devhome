// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.FileExplorerSourceControlIntegration.Services;

public partial class RepoStoreOptions
{
    private const string RepoStoreFileNameDefault = "TrackedRepositoryStore.json";

    public string RepoStoreFileName { get; set; } = RepoStoreFileNameDefault;

    private readonly string repoStoreFolderPathDefault = Path.Combine(Path.GetTempPath(), "FileExplorerSourceControlIntegration");

    private string? repoStoreFolderPath;

    public string RepoStoreFolderPath
    {
        get => repoStoreFolderPath is null ? repoStoreFolderPathDefault : repoStoreFolderPath;
        set => repoStoreFolderPath = string.IsNullOrEmpty(value) ? repoStoreFolderPathDefault : value;
    }
}
