// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Contracts;
using DevHome.Common.Environments.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Serilog;
using Windows.ApplicationModel.Activation;
using WinUIEx;

namespace DevHome.Common.Services;

public class NotificationService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(NotificationService));

    private readonly IWindowsIdentityService _windowsIdentityService;

    private readonly string _hyperVText = "Hyper-V";

    private readonly string _microsoftText = "Microsoft";

    private readonly StringResource _stringResource;

    private readonly WindowEx _windowEx;

    private StackedNotificationsBehavior? _notificationQueue;

    public NotificationService(IWindowsIdentityService windowsIdentityService, WindowEx windowEx)
    {
        _windowsIdentityService = windowsIdentityService;
        _windowEx = windowEx;
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
                    // Launch compmgmt.msc in PowerShell
                    var psi = new ProcessStartInfo();
                    psi.FileName = "powershell";
                    psi.Arguments = "Start-Process compmgmt.msc -Verb RunAs";
                    Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unable to launch computer management due to exception");
            }
        }
    }

    public void ShowRestartNotification()
    {
        if (_notificationQueue != null)
        {
            var command = new RelayCommand(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,

                    // Restart the computer
                    FileName = Environment.SystemDirectory + "\\shutdown.exe",
                    Arguments = "-r -t 0",
                    Verb = string.Empty,
                };

                var process = new Process
                {
                    StartInfo = startInfo,
                };
                process.Start();
            });

            _windowEx.DispatcherQueue.EnqueueAsync(() =>
                _notificationQueue?.Show(new Notification
                {
                    Title = _stringResource.GetLocalized("HyperVErrorTitle", _microsoftText, _hyperVText),
                    Message = _stringResource.GetLocalized("RestartMessage", _hyperVText),
                    Severity = InfoBarSeverity.Warning,
                    ActionButton = new Button
                    {
                        Content = _stringResource.GetLocalized("RestartButton"),
                        Command = command,
                    },
                }));
        }
        else
        {
            _log.Error("Notification queue is not initialized");
        }
    }

    private void ShowUnableToAddToHyperVAdminGroupNotification()
    {
        ShowNotificationAsync(
            _stringResource.GetLocalized("HyperVErrorTitle", _microsoftText, _hyperVText),
            _stringResource.GetLocalized("UserAddHyperVAdminFailed", _hyperVText),
            InfoBarSeverity.Warning).Wait();
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
                    var user = _windowsIdentityService.GetCurrentUserName();
                    if (user == null)
                    {
                        _log.Error("Unable to get the current user name");
                        return;
                    }

                    var startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = Environment.SystemDirectory + "\\net.exe";

                    // Add the user to the Hyper-V Administrators group
                    startInfo.Arguments = "localgroup \"Hyper-V Administrators\" " + user + " /add";
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";

                    var process = new System.Diagnostics.Process();
                    process.StartInfo = startInfo;

                    // Since a UAC prompt will be shown, we need to wait for the process to exit
                    // This can also be cancelled by the user which will result in an exception
                    try
                    {
                        process.Start();
                        process.WaitForExit();

                        CloseNotification(notification);

                        if (process.ExitCode == 0)
                        {
                            ShowRestartNotification();
                        }
                        else
                        {
                            ShowUnableToAddToHyperVAdminGroupNotification();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to add the user to the Hyper-V Administrators group");
                        ShowUnableToAddToHyperVAdminGroupNotification();
                    }
                });

                _windowEx.DispatcherQueue.EnqueueAsync(() =>
                {
                    notification = new Notification
                    {
                        Title = _stringResource.GetLocalized("HyperVErrorTitle", _microsoftText, _hyperVText),
                        Message = _stringResource.GetLocalized("UserNotInHyperAdminGroupMessage", _hyperVText),
                        Severity = InfoBarSeverity.Error,
                        ActionButton = new Button
                        {
                            Content = _stringResource.GetLocalized("HyperVAdminAddUser", _hyperVText),
                            Command = command,
                        },
                    };

                    _notificationQueue?.Show(notification);
                });
            }
            else
            {
                _log.Error("Notification queue is not initialized");
            }
        }
    }

    public void CloseNotification(Notification notification)
    {
        _windowEx.DispatcherQueue.EnqueueAsync(() => _notificationQueue?.Remove(notification));
    }

    public async Task ShowNotificationAsync(string title, string message, InfoBarSeverity severity)
    {
        await _windowEx.DispatcherQueue.EnqueueAsync(() =>
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Severity = severity,
                };

                _notificationQueue?.Show(notification);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "unable to show user notification message due to an exception");
            }
        });
    }

    public void CheckIfUserIsAHyperVAdmin()
    {
        if (!_windowsIdentityService.IsUserHyperVAdmin())
        {
            ShowHyperVAdminWarningToast();
        }
    }
}
