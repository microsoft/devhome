// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.DevInsights.Models;

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

public partial class Insight : ObservableObject
{
    internal string Title { get; set; } = string.Empty;

    internal string Description { get; set; } = string.Empty;

    internal InsightType InsightType { get; set; } = InsightType.Unknown;

    [ObservableProperty]
    private bool _hasBeenRead;

    // We show the badge by default, as HasBeenRead is false by default.
    [ObservableProperty]
    private double _badgeOpacity = 1;
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
