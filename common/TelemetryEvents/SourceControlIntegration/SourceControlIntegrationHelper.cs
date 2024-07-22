// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;

namespace DevHome.Common.TelemetryEvents.SourceControlIntegration;

public static class SourceControlIntegrationHelper
{
    private static readonly string[] Separator = new[] { "//" };

    public static string GetSafeRootPath(string rootPath)
    {
        var parts = rootPath.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault() ?? string.Empty;
    }
}
