// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.System;
using Windows.Win32.Foundation;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI.Helpers;

public enum ToolActivationType
{
    Protocol,
    Msix,
    Launch,
}

// ExternalTool represents an imported tool
public partial class ExternalTool : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalTool));

    public string ID { get; private set; }

    public string Name { get; private set; }

    public string Executable { get; private set; }

    [JsonConverter(typeof(EnumStringConverter<ToolActivationType>))]
    public ToolActivationType ActivationType { get; private set; } = ToolActivationType.Launch;

    public string Arguments { get; private set; }

    public string AppUserModelId { get; private set; }

    public string IconFilePath { get; private set; }

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    [property: JsonIgnore]
    private string _pinGlyph;

    [ObservableProperty]
    [property: JsonIgnore]
    private SoftwareBitmapSource? _toolIcon;

    [ObservableProperty]
    [property: JsonIgnore]
    private ImageIcon? _menuIcon;

    public ExternalTool(
        string name,
        string executable,
        ToolActivationType activationType,
        string arguments = "",
        string appUserModelId = "",
        string iconFilePath = "",
        bool isPinned = false)
    {
        Name = name;
        Executable = executable;
        ActivationType = activationType;
        Arguments = arguments;
        AppUserModelId = appUserModelId;
        IconFilePath = iconFilePath;
        IsPinned = isPinned;
        PinGlyph = IsPinned ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph;

        ID = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(executable))
        {
            GetIcons();
        }
    }

    partial void OnIsPinnedChanged(bool oldValue, bool newValue)
    {
        PinGlyph = newValue ? CommonHelper.UnpinGlyph : CommonHelper.PinGlyph;
    }

    private async void GetIcons()
    {
        try
        {
            if (!string.IsNullOrEmpty(IconFilePath))
            {
                ToolIcon = await GetSoftwareBitmapSourceFromImageFilePath(IconFilePath);
            }
            else
            {
                var softwareBitmap = GetSoftwareBitmapFromExecutable(Executable);
                if (softwareBitmap is not null)
                {
                    ToolIcon = await GetSoftwareBitmapSourceFromSoftwareBitmapAsync(softwareBitmap);
                }
            }

            MenuIcon = new ImageIcon
            {
                Source = ToolIcon,
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get tool image");
        }
    }

    internal async Task<bool> Invoke(int? pid, HWND? hwnd)
    {
        var result = false;

        try
        {
            var parsedArguments = string.Empty;
            if (!string.IsNullOrEmpty(Arguments))
            {
                var argumentVariables = new Dictionary<string, int>();
                if (pid.HasValue)
                {
                    argumentVariables.Add("pid", pid.Value);
                }

                if (hwnd.HasValue)
                {
                    argumentVariables.Add("hwnd", (int)hwnd.Value);
                }

                parsedArguments = ReplaceKnownVariables(Arguments, argumentVariables);
            }

            if (ActivationType == ToolActivationType.Protocol)
            {
                result = await Launcher.LaunchUriAsync(new Uri($"{parsedArguments}"));
            }
            else
            {
                if (ActivationType == ToolActivationType.Msix)
                {
                    var process = Process.Start("explorer.exe", $"shell:AppsFolder\\{AppUserModelId}");
                    result = process is not null;
                }
                else
                {
                    var startInfo = new ProcessStartInfo(Executable)
                    {
                        Arguments = parsedArguments,
                        UseShellExecute = true,
                    };
                    var process = Process.Start(startInfo);
                    result = process is not null;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Tool launched failed");
        }

        return result;
    }

    private string ReplaceKnownVariables(string input, Dictionary<string, int> argumentValues)
    {
        // Process the input string to replace any instance of defined variables with "real" values.
        // Eg, replace {pid} with 123, {hwnd} with 456.
        var pattern = @"\{(\w+)\}";

        var result = Regex.Replace(input, pattern, match =>
        {
            var variable = match.Groups[1].Value;

            // Check if the variable exists in the dictionary; if so, replace it.
            if (argumentValues.TryGetValue(variable, out var replacementValue))
            {
                return replacementValue.ToString(CultureInfo.InvariantCulture);
            }

            // If the variable is not found, keep it as is.
            return match.Value;
        });

        return result;
    }

    public void TogglePinnedState()
    {
        IsPinned = !IsPinned;
    }

    public void UnregisterTool()
    {
        ExternalToolsHelper.Instance.RemoveExternalTool(this);
    }
}
