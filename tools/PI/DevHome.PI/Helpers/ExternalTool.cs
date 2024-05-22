// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.Graphics.Imaging;
using Windows.Win32.Foundation;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI.Helpers;

public enum ExternalToolArgType
{
    None,
    ProcessId,
    Hwnd,
}

// ExternalTool represents an imported tool
public partial class ExternalTool : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalTool));

    public string ID { get; private set; }

    public string Name { get; private set; }

    public string Executable { get; private set; }

    [JsonConverter(typeof(EnumStringConverter<ExternalToolArgType>))]
    public ExternalToolArgType ArgType { get; private set; } = ExternalToolArgType.None;

    public string ArgPrefix
    {
        get; private set;
    }

    public string OtherArgs
    {
        get; private set;
    }

    [ObservableProperty]
    private bool _isPinned;

    // Note the additional "property:" syntax to ensure the JsonIgnore is propagated to the generated property.
    [ObservableProperty]
    [property: JsonIgnore]
    private SoftwareBitmapSource? _toolIcon;

    [ObservableProperty]
    [property: JsonIgnore]
    private BitmapIcon? _menuIcon;

    [JsonIgnore]
    private SoftwareBitmap? _softwareBitmap;

    public ExternalTool(
        string name,
        string executable,
        ExternalToolArgType argtype,
        string argprefix = "",
        string otherArgs = "",
        bool isPinned = false)
    {
        Name = name;
        Executable = executable;
        ArgType = argtype;
        ArgPrefix = argprefix;
        OtherArgs = otherArgs;
        IsPinned = isPinned;

        ID = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(executable))
        {
            GetToolImage();
            GetMenuIcon();
        }
    }

    private async void GetToolImage()
    {
        try
        {
            _softwareBitmap ??= GetSoftwareBitmapFromExecutable(Executable);
            if (_softwareBitmap is not null)
            {
                ToolIcon = await GetSoftwareBitmapSourceFromSoftwareBitmap(_softwareBitmap);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get tool image");
        }
    }

    private async void GetMenuIcon()
    {
        try
        {
            _softwareBitmap ??= GetSoftwareBitmapFromExecutable(Executable);
            if (_softwareBitmap is not null)
            {
                var bitmapUri = await SaveSoftwareBitmapToTempFile(_softwareBitmap);
                MenuIcon = new BitmapIcon
                {
                    UriSource = bitmapUri,
                    ShowAsMonochrome = false,
                };
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get menu icon");
        }
    }

    internal string CreateFullCommandLine(int? pid, HWND? hwnd)
    {
        return "\"" + Executable + "\"" + CreateCommandLine(pid, hwnd);
    }

    internal string CreateCommandLine(int? pid, HWND? hwnd)
    {
        var commandLine = $" {OtherArgs}";

        if (ArgType == ExternalToolArgType.Hwnd && hwnd is not null)
        {
            commandLine = $" {ArgPrefix} {hwnd:D} {OtherArgs}";
        }
        else if (ArgType == ExternalToolArgType.ProcessId && pid is not null)
        {
            commandLine = $" {ArgPrefix} {pid:D} {OtherArgs}";
        }

        return commandLine;
    }

    internal virtual Process? Invoke(int? pid, HWND? hwnd)
    {
        try
        {
            var toolProcess = new Process();
            toolProcess.StartInfo.FileName = Executable;
            toolProcess.StartInfo.Arguments = CreateCommandLine(pid, hwnd);
            toolProcess.StartInfo.UseShellExecute = false;
            toolProcess.StartInfo.RedirectStandardOutput = true;
            toolProcess.Start();
            return toolProcess;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Tool launched failed");
            return null;
        }
    }
}
