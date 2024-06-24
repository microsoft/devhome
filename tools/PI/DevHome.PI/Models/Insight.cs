// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace DevHome.PI.Models;

internal enum InsightType
{
    Unknown,
    LockedFile,
    AccessDeniedFile,
    AccessDeniedRegistry,
    InvalidPath,
    Security,
    MemoryViolation,
}

public sealed class Insight
{
    internal string Title { get; set; } = string.Empty;

    internal string Description { get; set; } = string.Empty;

    internal InsightType InsightType { get; set; } = InsightType.Unknown;

    internal bool IsExpanded { get; set; }
}

internal sealed class InsightRegex
{
    internal InsightType InsightType { get; set; }

    internal Regex Regex { get; set; }

    internal InsightRegex(InsightType type, Regex regex)
    {
        Regex = regex;
        InsightType = type;
    }
}
