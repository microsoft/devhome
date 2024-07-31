// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32.Foundation;

namespace DevHome.PI.Helpers;

[Flags]
public enum ToolType
{
    Unknown = 0,
    DumpAnalyzer = 1,
}

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

    [RelayCommand]
    public void Invoke()
    {
        InvokeTool(null, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd, null);
    }

    [RelayCommand]
    public void InvokeWithParent(Window parent)
    {
        InvokeTool(parent, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd, null);
    }

    public void InvokeWithParams(string commandLine)
    {
        InvokeTool(null, TargetAppData.Instance.TargetProcess?.Id, TargetAppData.Instance.HWnd, commandLine);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    internal async virtual void InvokeTool(Window? parentWindow, int? targetProcessId, HWND hWnd, string? commandLineParams) => throw new NotImplementedException();
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    [RelayCommand]
    public abstract void UnregisterTool();
}
