// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.ApplicationModel;

namespace DevHome.PI.Helpers;

internal sealed class CommonHelper
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(CommonHelper));

    internal static string GetLocalizedString(string stringName, params object[] args)
    {
        var stringResource = new StringResource();
        var localizedString = stringResource.GetLocalized(stringName, args);
        Debug.Assert(!string.IsNullOrEmpty(localizedString), stringName + " is empty. Check if " + stringName + " is present in Resources.resw.");
        return localizedString;
    }

    internal static void RunAsAdmin(int pid, string pageName)
    {
        var startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        var aliasSubDirectoryPath = $"Microsoft\\WindowsApps\\{Package.Current.Id.FamilyName}\\devhome.pi.exe";
        var aliasPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), aliasSubDirectoryPath);
        startInfo.FileName = aliasPath;

        // Pass pid and the page from where the admin request came from
        startInfo.Arguments = $"--pid {pid} --expandWindow {pageName}";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;

        // Since a UAC prompt will be shown, we need to wait for the process to exit
        // This can also be cancelled by the user which will result in an exception
        try
        {
            process.Start();

            // Close the primary window for this instance and exit
            var primaryWindow = Application.Current.GetService<PrimaryWindow>();
            primaryWindow.Close();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "UAC to run PI as admin was denied");
        }
    }
}
