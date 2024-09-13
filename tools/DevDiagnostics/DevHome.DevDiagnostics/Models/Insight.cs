// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.DevDiagnostics.Controls;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.Models;

public enum InsightType
{
    Unknown,
    LockedFile,
    AccessDeniedFile,
    AccessDeniedRegistry,
    InvalidPath,
    Security,
    MemoryViolation,
}

public abstract partial class Insight : ObservableObject
{
    public string Title { get; set; } = string.Empty;

    public InsightType InsightType { get; set; } = InsightType.Unknown;

    public UIElement? CustomControl { get; protected set; }

    [ObservableProperty]
    private bool _hasBeenRead;

    // We show the badge by default, as HasBeenRead is false by default.
    [ObservableProperty]
    private double _badgeOpacity = 1;
}

public class SimpleTextInsight : Insight
{
    private readonly InsightSimpleTextControl _mycontrol = new();
    private string _description = string.Empty;

    internal string Description
    {
        get => _description;

        set
        {
            _description = value;
            _mycontrol.Description = value;
        }
    }

    internal SimpleTextInsight()
    {
        CustomControl = _mycontrol;
    }
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
