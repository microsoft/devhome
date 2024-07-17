// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Contracts;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
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

    private readonly string _componentName = "ToastNotificationService";

    public bool WasHyperVAddToAdminGroupSuccessful { get; private set; }

    public ToastNotificationService(IWindowsIdentityService windowsIdentityService)
    {
        _windowsIdentityService = windowsIdentityService;
    }

    public bool ShowHyperVAdminWarningToast()
    {
        // Temporary toast notification to inform the user that they are not in the Hyper-V admin group.
        // In the future we'll use an admin process from Dev Home to add the user to the group.
        var toast = new AppNotificationBuilder()
           .AddText("Warning")
           .AddText(StringResourceHelper.GetResource(StringResourceHelper.UserNotInHyperAdminGroupMessage))
           .AddButton(new AppNotificationButton(StringResourceHelper.GetResource(StringResourceHelper.UserNotInHyperAdminGroupButton))
           .AddArgument("action", "AddUserToHyperVAdminGroup"))
           .BuildNotification();

        AppNotificationManager.Default.Show(toast);
        return toast.Id != 0;
    }

    public void HandlerNotificationActions(AppActivationArguments args)
    {
        if (args.Data is ToastNotificationActivatedEventArgs toastArgs)
        {
            try
            {
                if (toastArgs.Argument.Contains("action=AddUserToHyperVAdminGroup"))
                {
                    // Launch compmgmt.msc in powershell
                    var psi = new ProcessStartInfo();
                    psi.FileName = "powershell";
                    psi.Arguments = "Start-Process compmgmt.msc -Verb RunAs";
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError(_componentName, $"Unable to launch computer management due to exception", ex);
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
