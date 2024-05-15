// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
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
public class ExternalTool : INotifyPropertyChanged
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

    [JsonIgnore]
    private SoftwareBitmapSource? _toolIcon;

    [JsonIgnore]
    public SoftwareBitmapSource? ToolIcon
    {
        get => _toolIcon;
        private set
        {
            _toolIcon = value;
            OnPropertyChanged(nameof(ToolIcon));
        }
    }

    public ExternalTool(
        string name,
        string executable,
        ExternalToolArgType argtype,
        string argprefix = "",
        string otherArgs = "")
    {
        Name = name;
        Executable = executable;
        ArgType = argtype;
        ArgPrefix = argprefix;
        OtherArgs = otherArgs;

        ID = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(executable))
        {
            GetToolImage();
        }
    }

    private async void GetToolImage()
    {
        try
        {
            var toolIcon = System.Drawing.Icon.ExtractAssociatedIcon(Executable);

            // Fall back to Windows default app icon.
            toolIcon ??= System.Drawing.Icon.FromHandle(LoadDefaultAppIcon());

            if (toolIcon is not null)
            {
                ToolIcon = await WindowHelper.GetWinUI3BitmapSourceFromGdiBitmap(toolIcon.ToBitmap());
            }
        }
        catch (Exception ex)
        {
            _log.Debug(ex, "Unable to fetch tool image.");
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
