// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.DD.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.Models;

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

public class InsightPossibleLoaderIssue : Insight
{
    private readonly InsightForMissingFileProcessTerminationControl _mycontrol = new();
    private string _text = string.Empty;

    internal string Text
    {
        get => _text;

        set
        {
            _text = value;
            _mycontrol.Text = value;
        }
    }

    internal string ImageFileName { get; set; } = string.Empty;

    internal InsightPossibleLoaderIssue()
    {
        _mycontrol.Command = new RelayCommand(ConfigureLoaderSnaps);
        CustomControl = _mycontrol;
    }

    public void ConfigureLoaderSnaps()
    {
        try
        {
            FileInfo fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);

            var startInfo = new ProcessStartInfo()
            {
                FileName = "EnableLoaderSnaps.exe",
                Arguments = ImageFileName,
                UseShellExecute = true,
                WorkingDirectory = fileInfo.DirectoryName,
                Verb = "runas",
            };

            var process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
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
