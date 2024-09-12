// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using DevHome.DevDiagnostics.Models;
using DevHome.DevDiagnostics.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using Windows.ApplicationModel;

namespace DevHome.DevDiagnostics.Helpers;

public class ImageOptionsInternalTool : Tool
{
    private const string ButtonText = "\ue9d5"; // Checklist icon
    private static readonly string _toolName = CommonHelper.GetLocalizedString("ImageOptionsName");
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExternalTool));

    public ImageOptionsInternalTool()
        : base(_toolName, ToolType.Unknown, Settings.Default.IsImageOptionsToolPinned, false)
    {
        TargetAppData.Instance.PropertyChanged += TargetApp_PropertyChanged;
    }

    private void TargetApp_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TargetAppData.Instance.TargetProcess))
        {
            return;
        }

        var process = TargetAppData.Instance.TargetProcess;
        try
        {
            if (process is not null && process.MainModule is not null)
            {
                IsEnabled = true;
            }
            else
            {
                IsEnabled = false;
            }
        }
        catch (Win32Exception ex)
        {
            if (ex.NativeErrorCode == (int)Windows.Win32.Foundation.WIN32_ERROR.ERROR_ACCESS_DENIED)
            {
                IsEnabled = false;
            }
        }
    }

    public override IconElement GetIcon()
    {
        return new FontIcon
        {
            Glyph = ButtonText,
            FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
        };
    }

    internal override void InvokeTool(ToolLaunchOptions options)
    {
        if (TargetAppData.Instance.TargetProcess == null || TargetAppData.Instance.TargetProcess.MainModule == null)
        {
            return;
        }

        try
        {
            var fileInfo = new FileInfo(Environment.ProcessPath ?? string.Empty);
            Directory.SetCurrentDirectory(fileInfo.DirectoryName ?? string.Empty);

            var aliasRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Microsoft\\WindowsApps\\{Package.Current.Id.FamilyName}");

            var startInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(aliasRoot, "DevHome.IfeoTool.exe"),
                Arguments = TargetAppData.Instance.TargetProcess.MainModule.ModuleName,
                UseShellExecute = true,
                WorkingDirectory = fileInfo.DirectoryName,
                Verb = "runas",
            };

            var process = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
    }

    protected override void OnIsPinnedChange(bool newValue)
    {
        Settings.Default.IsImageOptionsToolPinned = newValue;
        Settings.Default.Save();
    }

    public override void UnregisterTool()
    {
        // Ignore this command for now until we have a way for the user to discover unregistered internal tools
    }
}
