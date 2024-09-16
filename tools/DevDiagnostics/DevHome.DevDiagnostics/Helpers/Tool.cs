// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32.Foundation;

namespace DevHome.DevDiagnostics.Helpers;

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

    [ObservableProperty]
    [property: JsonIgnore]
    private bool _isEnabled;

    public string Name { get; private set; }

    [JsonConverter(typeof(EnumStringConverter<ToolType>))]
    public ToolType Type { get; private set; }

    public Tool(string name, ToolType type, bool isPinned, bool isEnabled = true)
    {
        Name = name;
        Type = type;
        IsPinned = isPinned;
        PinGlyph = IsPinned ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph;
        IsEnabled = isEnabled;
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

    [property: JsonIgnore]
    [RelayCommand]
    public void TogglePinnedState()
    {
        IsPinned = !IsPinned;
    }

    public void Invoke(ToolLaunchOptions options)
    {
        InvokeTool(options);
    }

    [property: JsonIgnore]
    [RelayCommand]
    public void Invoke()
    {
        ToolLaunchOptions options = new();
        options.TargetProcessId = TargetAppData.Instance.TargetProcess?.Id;
        options.TargetHWnd = TargetAppData.Instance.HWnd;

        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        options.ParentWindow = barWindow;

        InvokeTool(options);
    }

    internal virtual void InvokeTool(ToolLaunchOptions options) => throw new NotImplementedException();

    [property: JsonIgnore]
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
