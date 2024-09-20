// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Windows.ApplicationModel.Activation;
using WinRT;

namespace DevHome.IfeoTool;

public static class Program
{
    [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
    [global::System.Runtime.InteropServices.DefaultDllImportSearchPaths(global::System.Runtime.InteropServices.DllImportSearchPath.SafeDirectories)]
    private static extern void XamlCheckProcessRequirements();

    [STAThread]
    public static void Main(string[] args)
    {
        Common.Logging.SetupLogging("appsettings_IfeoTool.json", "IfeoTool");

        try
        {
            XamlCheckProcessRequirements();

            WinRT.ComWrappersSupport.InitializeComWrappers();

            if (!IsRedirectionAsync().GetAwaiter().GetResult())
            {
                Log.Information("Starting application");
                Application.Start((p) =>
                {
                    var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                    var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
                    SynchronizationContext.SetSynchronizationContext(context);

                    var app = new IfeoToolApp();
                });
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Simple redirection logic to ensure only one instance of the tool per target app can run at a given time.
    private static async Task<bool> IsRedirectionAsync()
    {
        var targetAppName = string.Empty;
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (appArgs != null && appArgs.Kind == ExtendedActivationKind.Launch)
        {
            var activatedArgs = appArgs.Data.As<ILaunchActivatedEventArgs>();
            if (activatedArgs != null)
            {
                // Convert commandLine into a string array. We just can't split based just on spaces, in case there are spaces inclosed in quotes
                // i.e. --application "My App"
                var commandLineArgs = Regex.Matches(activatedArgs.Arguments, @"[\""].+?[\""]|[^ ]+").Select(m => m.Value).ToArray();
                targetAppName = commandLineArgs[1];
            }
        }

        AppInstance instance = AppInstance.FindOrRegisterForKey(targetAppName);
        if (!instance.IsCurrent)
        {
            await instance.RedirectActivationToAsync(appArgs);
            return true;
        }

        IfeoToolApp.TargetAppName = targetAppName;
        return false;
    }
}
