// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Customization.Helpers;

public partial class DevDriveCacheData
{
    public string? EnvironmentVariable { get; set; }

    public string? CacheName { get; set; }

    public List<string>? CacheDirectory { get; set; }

    public string? ExampleSubDirectory { get; set; }
}
