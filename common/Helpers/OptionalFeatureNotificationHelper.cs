// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Extensions;
using DevHome.Common.Scripts;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using static DevHome.Common.Scripts.ModifyWindowsOptionalFeatures;

namespace DevHome.Common.Helpers;

public partial class OptionalFeatureNotificationHelper
{
    private readonly ILogger _log;

    private readonly StackedNotificationsBehavior _notificationsHelper;

    private readonly StringResource _commonStringResource;

    public OptionalFeatureNotificationHelper(StackedNotificationsBehavior notificationsHelper, ILogger log)
    {
        _commonStringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");
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
                // The script successfully modified all features
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

    private void ShowRestartNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("ChangesAppliedTitle"),
            _commonStringResource.GetLocalized("RestartAfterChangesMessage"),
            InfoBarSeverity.Informational,
            RestartComputerCommand,
            _commonStringResource.GetLocalized("RestartButton"));
    }

    public void ShowNonAdminUserNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("NonAdminUserTitle"),
            _commonStringResource.GetLocalized("NonAdminUserMessage"),
            InfoBarSeverity.Informational);
    }

    private void ShowChangesNotAppliedNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("ChangesNotAppliedTitle"),
            _commonStringResource.GetLocalized("ChangesNotAppliedMessage"),
            InfoBarSeverity.Warning);
    }

    private void ShowFailedToApplyAllNotification()
    {
        _notificationsHelper?.ShowWithWindowExtension(
            _commonStringResource.GetLocalized("FailedToApplyChangesTitle"),
            _commonStringResource.GetLocalized("FailedToApplyChangesMessage"),
            InfoBarSeverity.Error,
            RestartComputerCommand,
            _commonStringResource.GetLocalized("RestartButton"));
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
