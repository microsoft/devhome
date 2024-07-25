// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileExplorerGitIntegration.Models;

public partial class GitExecutableConfigOptions
{
    private const string GitExecutableConfigFileNameDefault = "GitConfiguration.json";

    public string GitExecutableConfigFileName { get; set; } = GitExecutableConfigFileNameDefault;

    private readonly string gitExecutableConfigFolderPathDefault = Path.Combine(Path.GetTempPath(), "FileExplorerGitIntegration");

    private string? gitExecutableConfigFolderPath;

    public string GitExecutableConfigFolderPath
    {
        get => gitExecutableConfigFolderPath is null ? gitExecutableConfigFolderPathDefault : gitExecutableConfigFolderPath;
        set => gitExecutableConfigFolderPath = string.IsNullOrEmpty(value) ? gitExecutableConfigFolderPathDefault : value;
    }
}
