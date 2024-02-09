// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.ApplicationModel.Activation;
using Windows.Media.AppBroadcasting;
using WinUIEx.Messaging;

namespace DevHome.Common.Services;

public class ToastNotificationService
{
    public bool ShowHyperVAdminWarningToast()
    {
        // To Do: Add localization
        var toast = new AppNotificationBuilder()
           .AddText("Warning")
           .AddText("The current user is not a Hyper-V administrator. Hyper-V Virtual machines will not load. Please add user to the Hyper-V Administrators group.")
           .AddButton(new AppNotificationButton("Open Computer Management")
           .AddArgument("action", "lauchComputerManagement"))
           .BuildNotification();

        AppNotificationManager.Default.Show(toast);
        return toast.Id != 0;
    }

    public bool ShowSimpleToast(string title, string message)
    {
        var toast = new AppNotificationBuilder()
            .AddText(title)
           .AddText(message)
           .BuildNotification();

        AppNotificationManager.Default.Show(toast);
        return toast.Id != 0;
    }

    public void HandlerNotificationActions(AppActivationArguments args)
    {
        if (args.Data is ToastNotificationActivatedEventArgs toastArgs)
        {
            if (toastArgs.Argument.Contains("action=lauchComputerManagement"))
            {
                // Launch compmgmt.msc in powershell
                var psi = new ProcessStartInfo();
                psi.FileName = "powershell";
                psi.Arguments = "Start-Process compmgmt.msc -Verb RunAs";
                Process.Start(psi);
                return;
            }
        }
    }
}
