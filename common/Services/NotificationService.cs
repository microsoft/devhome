// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Contracts;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.ApplicationModel.Activation;
using Windows.Media.AppBroadcasting;
using WinUIEx.Messaging;

namespace DevHome.Common.Services;

public class NotificationService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ToastNotificationService));
    private readonly IWindowsIdentityService _windowsIdentityService;

    private readonly string _componentName = "NotificationService";

    private readonly StringResource _stringResource;

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private StackedNotificationsBehavior? _notificationQueue;

    public NotificationService(IWindowsIdentityService windowsIdentityService)
    {
        _windowsIdentityService = windowsIdentityService;
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _stringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");
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

    public void Initialize(StackedNotificationsBehavior notificationQueue)
    {
        _notificationQueue = notificationQueue;
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
                _log.Error(_componentName, $"Unable to launch computer management due to exception", ex);
            }
        }
    }

    public void ShowRestartNotification()
    {
        if (_notificationQueue != null)
        {
            var notification = new Notification();

            var command = new RelayCommand(() =>
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";

                // Restart the computer
                startInfo.Arguments = "/C shutdown -r -t 0";
                startInfo.Verb = string.Empty;
                process.StartInfo = startInfo;
                process.Start();
            });

            _dispatcher.EnqueueAsync(() =>
            {
                notification = new Notification
                {
                    Title = _stringResource.GetLocalized("HyperVErrorTitle"),
                    Message = _stringResource.GetLocalized("RestartMessage"),
                    Severity = InfoBarSeverity.Warning,
                    ActionButton = new Button
                    {
                        Content = _stringResource.GetLocalized("RestartButton"),
                        Command = command,
                    },
                };

                _notificationQueue?.Show(notification);
            });
        }
        else
        {
            Log.Logger()?.ReportError(_componentName, "Notification queue is not initialized");
        }
    }

    public void CheckIfUserIsAHyperVAdminAndShowNotification()
    {
        if (!_windowsIdentityService.IsUserHyperVAdmin())
        {
            if (_notificationQueue != null)
            {
                var notification = new Notification();

                var command = new RelayCommand(() =>
                {
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";

                    var user = _windowsIdentityService.GetCurrentUserName();
                    if (user == null)
                    {
                        Log.Logger()?.ReportError(_componentName, "Unable to get the current user name");
                        return;
                    }

                    // Add the user to the Hyper-V Administrators group
                    startInfo.Arguments = "/C net localgroup \"Hyper-V Administrators\" " + user + " /add";
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    process.StartInfo = startInfo;
                    process.Start();

                    // To Do: Check process exit code
                    process.WaitForExit();

                    // Close the notification
                    CloseNotification(notification);
                    ShowRestartNotification();
                });

                _dispatcher.EnqueueAsync(() =>
                {
                    notification = new Notification
                    {
                        Title = _stringResource.GetLocalized("HyperVErrorTitle"),
                        Message = _stringResource.GetLocalized("UserNotInHyperAdminGroupMessage"),
                        Severity = InfoBarSeverity.Error,
                        ActionButton = new Button
                        {
                            Content = _stringResource.GetLocalized("HyperVAdminAddUser"),
                            Command = command,
                        },
                    };

                    _notificationQueue?.Show(notification);
                });
            }
            else
            {
                Log.Logger()?.ReportError(_componentName, "Notification queue is not initialized");
            }
        }
    }

    public void CloseNotification(Notification notification)
    {
        _dispatcher.EnqueueAsync(() => _notificationQueue?.Remove(notification));
    }

    public async Task ShowNotificationAsync(string title, string message, InfoBarSeverity severity)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Severity = severity,
        };

        await _dispatcher.EnqueueAsync(() => _notificationQueue?.Show(notification));
    }

    public void CheckIfUserIsAHyperVAdmin()
    {
        if (!_windowsIdentityService.IsUserHyperVAdmin())
        {
            ShowHyperVAdminWarningToast();
        }
    }
}
>>>>>>> be7ce1de (Added InfoBar):common/Services/NotificationService.cs
