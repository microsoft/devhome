﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Customization.ViewModels.DevDriveInsights;

public partial class DevDriveCacheViewModel
{
    public string? EnvironmentVariable { get; set; }

    public string? CacheName { get; set; }

    public string? CacheDirectory { get; set; }

    public string? ExampleDirectory { get; set; }
}
