// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        ToolType type = ToolType.Unknown,
        bool isPinned = false)
        : base(name, type, isPinned)
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

    internal async override void InvokeTool(ToolLaunchOptions options)
    {
        try
        {
            await InvokeToolInternal(options);
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

    internal void ValidateOptions(ToolLaunchOptions options)
    {
        // We can only get stdout if we're launching the process via CreateProcess.
        if (options.RedirectStandardOut && ActivationType != ToolActivationType.Launch)
        {
            throw new InvalidOperationException("Can only redirect StandardOut with a CreateProcess launch");
        }
    }

    private string CreateCommandLine(ToolLaunchOptions options)
    {
        var commandLineArgs = string.Empty;
        if (!string.IsNullOrEmpty(Arguments))
        {
            var argumentVariables = new Dictionary<string, string>();
            if (options.TargetProcessId.HasValue)
            {
                argumentVariables.Add("pid", options.TargetProcessId.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (options.TargetHWnd != HWND.Null)
            {
                argumentVariables.Add("hwnd", ((int)options.TargetHWnd).ToString(CultureInfo.InvariantCulture));
            }

            if (Type.HasFlag(ToolType.DumpAnalyzer) && options.CommandLineParams is not null)
            {
                argumentVariables.Add("crashDumpPath", options.CommandLineParams);
            }

            commandLineArgs = ReplaceKnownVariables(Arguments, argumentVariables);
        }

        return commandLineArgs;
    }

    internal async Task<Process?> InvokeToolInternal(ToolLaunchOptions options)
    {
        var process = default(Process);

        ValidateOptions(options);

        string commandLineArgs = CreateCommandLine(options);

        try
        {
            if (ActivationType == ToolActivationType.Protocol)
            {
                // Docs say this returns true if the default app for the URI scheme was launched;
                // false otherwise. However, if there's no registered app for the protocol, it shows
                // the "get an app from the store" dialog, and returns true. So we can't rely on the
                // return value to know if the tool was actually launched.
                var result = await Launcher.LaunchUriAsync(new Uri(commandLineArgs));
                if (result != true)
                {
                    // We get here if the user supplied a valid registered protocol, but the app failed to launch.
                    var errorMessage = string.Format(
                        CultureInfo.InvariantCulture, _errorMessageText, commandLineArgs);
                    throw new InvalidOperationException(errorMessage);
                }

                // Currently we can't get the process object of the launched app via LaunchUriAsync. If we
                // ever do and want to populate ToolLaunchOptions.LaunchedProcess, we'll need to revisit the async behavior here,
                // as when we return from this function (and our async operation is still ongoing) our callers currently expect to
                // be able to read ToolLaunchOptions.LaunchedProcess immedately... there isn't an indication that they would need to
                // do an await first.
                // process = ProcessLaunchedFromLaunchUriAsync;
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
                        finalArguments = $"{Executable} {commandLineArgs}";
                    }
                    else if (Path.GetExtension(Executable).Equals(".ps1", StringComparison.OrdinalIgnoreCase))
                    {
                        // Note: running powershell scripts might require setting the execution policy.
                        finalExecutable = "powershell.exe";
                        finalArguments = $"{Executable} {commandLineArgs}";
                    }
                    else
                    {
                        finalExecutable = Executable;
                        finalArguments = commandLineArgs;
                    }

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = finalExecutable,
                        Arguments = finalArguments,

                        // If we want to redirect standard out, we can't use shell execute
                        UseShellExecute = !options.RedirectStandardOut,
                        RedirectStandardOutput = options.RedirectStandardOut,
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

        options.LaunchedProcess = process;
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

    private string ReplaceKnownVariables(string input, Dictionary<string, string> argumentValues)
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
                return replacementValue;
            }

            // If the variable is not found, keep it as is.
            return match.Value;
        });

        return result;
    }

    public override void UnregisterTool()
    {
        Application.Current.GetService<ExternalToolsHelper>().RemoveExternalTool(this);
    }
}
