// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Environments.Models;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Environments.Helpers;

public partial class EnvironmentsNotificationHelper
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(EnvironmentsNotificationHelper));

    private readonly WindowsIdentityService _windowsIdentityService = new();

    private readonly string _microsoftHyperVText = "Microsoft Hyper-V";

    private readonly StringResource _stringResource;

    public StackedNotificationsBehavior StackedNotificationsBehavior { get; set; }

    public EnvironmentsNotificationHelper(StackedNotificationsBehavior notificationsQueue)
    {
        StackedNotificationsBehavior = notificationsQueue;
        _stringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");
    }

    public void DisplayComputeSystemEnumerationErrors(ComputeSystemsLoadedData computeSystemData)
    {
        var extensionId = computeSystemData.ProviderDetails.ExtensionWrapper.ExtensionClassId;

        if (extensionId.Equals(CommonConstants.HyperVExtensionClassId, StringComparison.OrdinalIgnoreCase) &&
            !_windowsIdentityService.IsUserHyperVAdmin())
        {
            ShowAddUserToAdminGroupNotification();
        }

        // Show error notifications for failed provider/developer id combinations
        var provider = computeSystemData.ProviderDetails.ComputeSystemProvider;

        foreach (var mapping in computeSystemData.DevIdToComputeSystemMap.Where(kv =>
            kv.Value.Result.Status == ProviderOperationStatus.Failure))
        {
            var result = mapping.Value.Result;
            StackedNotificationsBehavior.ShowWithWindowExtension(provider.DisplayName, result.DisplayMessage, InfoBarSeverity.Error);

            _log.Error($"Error after retrieving Compute systems for provider: " +
                $"Provider Id: {provider.Id} \n" +
                $"DisplayText: {result.DisplayMessage} \n" +
                $"DiagnosticText: {result.DiagnosticText} \n" +
                $"ExtendedError: {result.ExtendedError}");
        }
    }

    private void ShowAddUserToAdminGroupNotification()
    {
        StackedNotificationsBehavior.ShowWithWindowExtension(
            _microsoftHyperVText,
            _stringResource.GetLocalized("UserNotInHyperAdminGroupMessage"),
            InfoBarSeverity.Error,
            AddUserToHyperVAdminGroupCommand,
            _stringResource.GetLocalized("HyperVAdminAddUser"));
    }

    public void ShowHyperVRestartNotification()
    {
        StackedNotificationsBehavior.ShowWithWindowExtension(
            _microsoftHyperVText,
            _stringResource.GetLocalized("HyperVAdminGroupRestartMessage"),
            InfoBarSeverity.Warning,
            RestartComputerCommand,
            _stringResource.GetLocalized("RestartButton"));
    }

    private void ShowUnableToAddToHyperVAdminGroupNotification()
    {
        StackedNotificationsBehavior.ShowWithWindowExtension(
            _microsoftHyperVText,
            _stringResource.GetLocalized("UserAddHyperVAdminFailed"),
            InfoBarSeverity.Warning);
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

    [RelayCommand]
    private void AddUserToHyperVAdminGroup(Notification notification)
    {
        var user = _windowsIdentityService.GetCurrentUserName();
        if (user == null)
        {
            _log.Error("Unable to get the current user name");
            return;
        }

        var startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = $"{Environment.SystemDirectory} \\net.exe";

        // Add the user to the Hyper-V Administrators group
        startInfo.Arguments = $"localgroup \"Hyper-V Administrators\" {user} /add";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;

        // Since a UAC prompt will be shown, we need to wait for the process to exit
        // This can also be cancelled by the user which will result in an exception
        try
        {
            process.Start();
            process.WaitForExit();

            StackedNotificationsBehavior.RemoveWithWindowExtension(notification);

            if (process.ExitCode == 0)
            {
                ShowHyperVRestartNotification();
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
    }
}
