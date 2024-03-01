// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ABI.System;
using DevHome.Common.Contracts;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.ApplicationModel.Activation;
using Windows.Media.AppBroadcasting;
using WinUIEx.Messaging;

namespace DevHome.Common.Services;

public class ToastNotificationService
{
    private readonly IWindowsIdentityService _windowsIdentityService;

    public ToastNotificationService(IWindowsIdentityService windowsIdentityService)
    {
        _windowsIdentityService = windowsIdentityService;
    }

    public bool ShowHyperVAdminWarningToast()
    {
        // To Do: Add localization
        var toast = new AppNotificationBuilder()
           .AddText("Warning")
           .AddText("The current user is not a Hyper-V administrator. Hyper-V Virtual machines will not load. Please add the user to the Hyper-V Administrators group and reboot.")
           .AddButton(new AppNotificationButton("Add user to the Hyper-V Admin group and enable Hyper-V")
           .AddArgument("action", "AddUserToHyperVAdminGroup"))
           .BuildNotification();

        AppNotificationManager.Default.Show(toast);
        return toast.Id != 0;
    }

    public void HandlerNotificationActions(AppActivationArguments args)
    {
        if (args.Data is ToastNotificationActivatedEventArgs toastArgs)
        {
            if (toastArgs.Argument.Contains("action=AddUserToHyperVAdminGroup"))
            {
                // Launch powershell and add user to the Hyper-V admin group and enable hyper-v.
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.Verb = "RunAs";
                processStartInfo.FileName = "powershell.exe";
                processStartInfo.ArgumentList.Add("net localgroup \"Hyper-V Administrators\" (whoami) /add");
                processStartInfo.ArgumentList.Add("Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All -NoRestart");
                Process.Start(processStartInfo);
                return;
            }
        }
    }

    public void CheckIfUserIsAHyperVAdmin()
    {
        if (!_windowsIdentityService.IsUserHyperVAdmin())
        {
            ShowHyperVAdminWarningToast();
        }
    }
}
