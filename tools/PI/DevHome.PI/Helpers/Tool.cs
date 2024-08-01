// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32.Foundation;

namespace DevHome.PI.Helpers;

public abstract partial class Tool : ObservableObject
{
    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _pinGlyph;

    [ObservableProperty]
    [property: JsonIgnore]
    private ImageSource? _toolIconSource;

    public string Name { get; private set; }

    public ToolType Type { get; private set; }

    public Tool(string name, ToolType type, bool isPinned)
    {
        Name = name;
        Type = type;
        IsPinned = isPinned;
        PinGlyph = IsPinned ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph;
    }

    public abstract IconElement GetIcon();

    partial void OnIsPinnedChanged(bool oldValue, bool newValue)
    {
        PinGlyph = newValue ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph;
        OnIsPinnedChange(newValue);
    }

    protected virtual void OnIsPinnedChange(bool newValue)
    {
    }

    [RelayCommand]
    public void TogglePinnedState()
    {
        IsPinned = !IsPinned;
    }

    public void Invoke(ToolLaunchOptions options)
    {
        InvokeTool(options);
    }

    [RelayCommand]
    public void Invoke()
    {
        ToolLaunchOptions options = new();
        options.TargetProcessId = TargetAppData.Instance.TargetProcess?.Id;
        options.TargetHWnd = TargetAppData.Instance.HWnd;
        InvokeTool(options);
    }

    internal virtual void InvokeTool(ToolLaunchOptions options) => throw new NotImplementedException();

    [RelayCommand]
    public abstract void UnregisterTool();
}

[Flags]
public enum ToolType
{
    Unknown = 0,
    DumpAnalyzer = 1,
}

public class ToolLaunchOptions
{
    public Window? ParentWindow { get; set; }

    public bool RedirectStandardOut { get; set; } /* = false; */

    public string? CommandLineParams { get; set; }

    public int? TargetProcessId { get; set; }

    internal HWND TargetHWnd { get; set; }

    public Process? LaunchedProcess { get; set; }
}
