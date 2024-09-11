// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Customization.Helpers;

public partial class DevDriveCacheData
{
    public string? EnvironmentVariable { get; set; }

    public string? CacheName { get; set; }

    public List<string>? CacheDirectory { get; set; }

    public List<string>? RelatedCacheDirectories { get; set; } = new();

    public List<string>? RelatedEnvironmentVariables { get; set; } = new();

    public string? ExampleSubDirectory { get; set; }
}
