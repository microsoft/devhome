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

    private readonly string _repoStoreFolderPathDefault = Path.Combine(Path.GetTempPath(), "FileExplorerSourceControlIntegration");

    private string? _repoStoreFolderPath;

    public string RepoStoreFolderPath
    {
        get => _repoStoreFolderPath is null ? _repoStoreFolderPathDefault : _repoStoreFolderPath;
        set => _repoStoreFolderPath = string.IsNullOrEmpty(value) ? _repoStoreFolderPathDefault : value;
    }
}
