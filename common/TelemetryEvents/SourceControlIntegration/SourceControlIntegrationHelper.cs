// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace DevHome.Common.TelemetryEvents.SourceControlIntegration;

public static class SourceControlIntegrationHelper
{
    public static string GetSafeRootPath(string rootPath)
    {
        var parts = rootPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault() ?? string.Empty;
    }
}
