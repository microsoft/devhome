// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Serilog;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.WindowsAndMessaging;
using static DevHome.PI.Helpers.WindowHelper;

namespace DevHome.PI.Helpers;

public enum ToolActivationType
{
    Protocol,
    Msix,
    Launch,
}

public partial class ExternalTool : Tool
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalTool));
    private readonly string _errorTitleText = CommonHelper.GetLocalizedString("ToolLaunchErrorTitle");

    private readonly string _errorMessageText = CommonHelper.GetLocalizedString("ToolLaunchErrorMessage");

    public string ID { get; private set; }

    public string Executable { get; private set; }

    [JsonConverter(typeof(EnumStringConverter<ToolActivationType>))]
    public ToolActivationType ActivationType { get; private set; } = ToolActivationType.Launch;

    public string Arguments { get; private set; }

    public string AppUserModelId { get; private set; }

    public string IconFilePath { get; private set; }

    public ExternalTool(
        string name,
        string executable,
        ToolActivationType activationType,
        string arguments = "",
        string appUserModelId = "",
        string iconFilePath = "",
        bool isPinned = false)
        : base(name, isPinned)
    {
        Executable = executable;
        ActivationType = activationType;
        Arguments = arguments;
        AppUserModelId = appUserModelId;
        IconFilePath = iconFilePath;

        ID = Guid.NewGuid().ToString();

        if (!string.IsNullOrEmpty(executable))
        {
            GetIcons();
        }
    }

    private async void GetIcons()
    {
        try
        {
            if (!string.IsNullOrEmpty(IconFilePath))
            {
                ToolIconSource = await GetSoftwareBitmapSourceFromImageFilePath(IconFilePath);
            }
            else
            {
                var softwareBitmap = GetSoftwareBitmapFromExecutable(Executable);
                if (softwareBitmap is not null)
                {
                    ToolIconSource = await GetSoftwareBitmapSourceFromSoftwareBitmapAsync(softwareBitmap);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get tool image");
        }
    }

    public override IconElement GetIcon()
    {
        return new ImageIcon
        {
            Source = ToolIconSource,
        };
    }

    internal async override void InvokeTool(Window? parentWindow, int? targetProcessId, HWND hWnd)
    {
        try
        {
            var process = await InvokeToolInternal(targetProcessId, hWnd);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Tool launched failed");

            var builder = new StringBuilder();
            builder.AppendLine(ex.Message);
            if (ex.InnerException is not null)
            {
                builder.AppendLine(ex.InnerException.Message);
            }

            var errorMessage = string.Format(CultureInfo.CurrentCulture, builder.ToString(), Executable);
            PInvoke.MessageBox(HWND.Null, errorMessage, _errorTitleText, MESSAGEBOX_STYLE.MB_ICONERROR);
        }
    }

    internal async Task<Process?> InvokeToolInternal(int? pid, HWND? hwnd)
    {
        var process = default(Process);

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

        try
        {
            if (ActivationType == ToolActivationType.Protocol)
            {
                // Docs say this returns true if the default app for the URI scheme was launched;
                // false otherwise. However, if there's no registered app for the protocol, it shows
                // the "get an app from the store" dialog, and returns true. So we can't rely on the
                // return value to know if the tool was actually launched.
                var result = await Launcher.LaunchUriAsync(new Uri(parsedArguments));
                if (result != true)
                {
                    // We get here if the user supplied a valid registered protocol, but the app failed to launch.
                    var errorMessage = string.Format(
                        CultureInfo.InvariantCulture, _errorMessageText, parsedArguments);
                    throw new InvalidOperationException(errorMessage);
                }
            }
            else
            {
                if (ActivationType == ToolActivationType.Msix)
                {
                    process = LaunchPackagedTool(AppUserModelId);
                }
                else
                {
                    var finalExecutable = string.Empty;
                    var finalArguments = string.Empty;

                    if (Path.GetExtension(Executable).Equals(".msc", StringComparison.OrdinalIgnoreCase))
                    {
                        // Note: running most msc files requires elevation.
                        finalExecutable = "mmc.exe";
                        finalArguments = $"{Executable} {parsedArguments}";
                    }
                    else if (Path.GetExtension(Executable).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Note: running powershell scripts might require setting the execution policy.
                        finalExecutable = "powershell.exe";
                        finalArguments = $"{Executable} {parsedArguments}";
                    }
                    else
                    {
                        finalExecutable = Executable;
                        finalArguments = parsedArguments;
                    }

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = finalExecutable,
                        Arguments = finalArguments,
                        UseShellExecute = true,
                    };
                    process = Process.Start(startInfo);
                }
            }
        }
        catch (Exception ex)
        {
            // We compose a custom exception because an exception from executing some tools
            // (powershell, mmc) will have lost the target tool information.
            var errorMessage = string.Format(CultureInfo.InvariantCulture, _errorMessageText, Executable);
            throw new InvalidOperationException(errorMessage, ex);
        }

        return process;
    }

    public static Process? LaunchPackagedTool(string appUserModelId)
    {
        var process = default(Process);
        var clsid = CLSID.ApplicationActivationManager;
        var iid = typeof(IApplicationActivationManager).GUID;
        object obj;

        int hr = PInvoke.CoCreateInstance(
            in clsid, null, CLSCTX.CLSCTX_LOCAL_SERVER, in iid, out obj);

        if (HResult.Succeeded(hr))
        {
            var appActiveManager = (IApplicationActivationManager)obj;
            uint processId;
            hr = appActiveManager.ActivateApplication(
                appUserModelId, string.Empty, ACTIVATEOPTIONS.None, out processId);
            if (HResult.Succeeded(hr))
            {
                process = Process.GetProcessById((int)processId);
            }
        }
        else
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return process;
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

    public override void UnregisterTool()
    {
        ExternalToolsHelper.Instance.RemoveExternalTool(this);
    }
}
