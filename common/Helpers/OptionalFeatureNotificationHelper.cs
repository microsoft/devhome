// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Extensions;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using static DevHome.Common.Scripts.ModifyWindowsOptionalFeatures;

namespace DevHome.Common.Helpers;

public partial class OptionalFeatureNotificationHelper
{
    private readonly ILogger _log;

    private readonly Window _window;

    private readonly StackedNotificationsBehavior _notificationsHelper;

    private readonly StringResource _commonStringResource;

    public OptionalFeatureNotificationHelper(Window window, StackedNotificationsBehavior notificationsHelper, ILogger log)
    {
        _commonStringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");
        _window = window;
        _notificationsHelper = notificationsHelper;
        _log = log;
    }

    public void HandleModifyFeatureResult(ModifyWindowsOptionalFeatures.ExitCode exitCode)
    {
        _notificationsHelper?.ClearWithWindowExtension();
        _log.Information($"Script exited with code: '{exitCode}'");

        switch (exitCode)
        {
            case ExitCode.Success:
                // The script successfully modified all features but a restart is required.
                ShowRestartNotification();
                return;
            case ExitCode.NoChange:
                // The script found that nothing needed to be done. The features will be reloaded
                // to show the correct state.
                return;
            case ExitCode.Failure:
            default:
                // Script failed to modify features.
                ShowFailedToApplyAllNotification();
                return;
        }
    }

    private async void ShowRestartNotification()
    {
        await _window.DispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = _commonStringResource.GetLocalized("ChangesAppliedTitle"),
                Content = _commonStringResource.GetLocalized("RestartAfterChangesMessage"),
                CloseButtonText = _commonStringResource.GetLocalized("DoNotRestartButton"),
                PrimaryButtonText = _commonStringResource.GetLocalized("RestartButton"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _window.Content.XamlRoot,
            };

            dialog.PrimaryButtonClick += (sender, args) =>
            {
                RestartComputer();
            };

            await dialog.ShowAsync();
        });
    }

    public void ShowNonAdminUserNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("NonAdminUserTitle"),
            _commonStringResource.GetLocalized("NonAdminUserMessage"),
            InfoBarSeverity.Warning);
    }

    private void ShowFailedToApplyAllNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("FailedToApplyChangesTitle"),
            _commonStringResource.GetLocalized("FailedToApplyChangesMessage"),
            InfoBarSeverity.Error);
    }

    [RelayCommand]
    private void RestartComputer()
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
    }
}
