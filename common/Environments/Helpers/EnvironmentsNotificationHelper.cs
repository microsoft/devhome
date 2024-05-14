// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Scripts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Common.Environments.Helpers;

/// <summary>
/// Helper class for the pages in Dev Home that display environments. These pages can use this helper to display
/// the Community toolkits <see cref="StackedNotificationsBehavior"/> which allows info bars to be shown in a queue
/// like manner.
/// </summary>
public partial class EnvironmentsNotificationHelper
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(EnvironmentsNotificationHelper));

    private readonly WindowsIdentityHelper _windowsIdentityHelper = new();

    private const string MicrosoftHyperVText = "Microsoft Hyper-V";

    private static bool _shouldShowHyperVRebootButton;

    private readonly StringResource _stringResource;

    private readonly StackedNotificationsBehavior _stackedNotificationsBehavior;

    public EnvironmentsNotificationHelper(StackedNotificationsBehavior notificationsQueue)
    {
        _stackedNotificationsBehavior = notificationsQueue;
        _stringResource = new StringResource("DevHome.Common.pri", "DevHome.Common/Resources");
    }

    public void DisplayComputeSystemProviderErrors(ComputeSystemProviderDetails providerDetails)
    {
        var extensionId = providerDetails.ExtensionWrapper.ExtensionClassId;

        // When the Hyper-V feature is not present it will never be queried for its compute systems.
        // So it is safe to assume that when we enter this if statement the feature is available on the machine
        if (extensionId.Equals(CommonConstants.HyperVExtensionClassId, StringComparison.OrdinalIgnoreCase))
        {
            ShowAddUserToAdminGroupAndEnableHyperVNotification();
        }
    }

    public void DisplayComputeSystemEnumerationErrors(ComputeSystemsLoadedData computeSystemData)
    {
        DisplayComputeSystemProviderErrors(computeSystemData.ProviderDetails);

        // Show error notifications for failed provider/developer id combinations
        var provider = computeSystemData.ProviderDetails.ComputeSystemProvider;

        foreach (var mapping in computeSystemData.DevIdToComputeSystemMap.Where(kv =>
            kv.Value.Result.Status == ProviderOperationStatus.Failure))
        {
            var result = mapping.Value.Result;
            _stackedNotificationsBehavior.ShowWithWindowExtension(provider.DisplayName, result.DisplayMessage, InfoBarSeverity.Error);

            _log.Error($"Error after retrieving Compute systems for provider: " +
                $"Provider Id: {provider.Id} \n" +
                $"DisplayText: {result.DisplayMessage} \n" +
                $"DiagnosticText: {result.DiagnosticText} \n" +
                $"ExtendedError: {result.ExtendedError}");
        }
    }

    public void ClearNotifications()
    {
        _stackedNotificationsBehavior.ClearWithWindowExtension();
    }

    private void ShowAddUserToAdminGroupAndEnableHyperVNotification()
    {
        // If we've already added the user to the group, their local security access token won't be updated
        // until the user logs off and back on again. With enabling Hyper-V, the feature won't be active until
        // the user reboots. If they choose not to reboot then we don't want to prompt
        // them to be enable it or added again so we'll prompt them to reboot again.
        if (_shouldShowHyperVRebootButton)
        {
            ShowRestartNotification();
            return;
        }

        var userInAdminGroup = _windowsIdentityHelper.IsUserHyperVAdmin();
        var featureEnabled = ManagementInfrastructureHelper.IsWindowsFeatureAvailable(CommonConstants.HyperVWindowsOptionalFeatureName) == FeatureAvailabilityKind.Enabled;

        if (!featureEnabled && !userInAdminGroup)
        {
            // Notification to enable Hyper-V and add user to Admin group
            _stackedNotificationsBehavior.ShowWithWindowExtension(
                MicrosoftHyperVText,
                _stringResource.GetLocalized("HyperVAdminAddUserAndEnableHyperVMessage"),
                InfoBarSeverity.Error,
                AddUserToHyperVAdminGroupAndEnableHyperVCommand,
                _stringResource.GetLocalized("HyperVAdminAddUserAndEnableHyperVButton"));
        }
        else if (!featureEnabled && userInAdminGroup)
        {
            // Notification to enable the Hyper-V feature when user is in the admin group
            _stackedNotificationsBehavior.ShowWithWindowExtension(
                MicrosoftHyperVText,
                _stringResource.GetLocalized("HyperVFeatureNotEnabledMessage"),
                InfoBarSeverity.Error,
                AddUserToHyperVAdminGroupAndEnableHyperVCommand,
                _stringResource.GetLocalized("HyperVEnableButton"));
        }
        else if (featureEnabled && !userInAdminGroup)
        {
            // Notification to add user to the Hyper-V admin group when the feature is enabled
            _stackedNotificationsBehavior.ShowWithWindowExtension(
                MicrosoftHyperVText,
                _stringResource.GetLocalized("UserNotInHyperAdminGroupMessage"),
                InfoBarSeverity.Error,
                AddUserToHyperVAdminGroupAndEnableHyperVCommand,
                _stringResource.GetLocalized("HyperVAdminAddUserButton"));
        }
    }

    private void ShowErrorWithRebootAfterExecutionMessage(string errorMsg)
    {
        _stackedNotificationsBehavior.ShowWithWindowExtension(
            MicrosoftHyperVText,
            errorMsg,
            InfoBarSeverity.Error);
    }

    private void ShowRestartNotification()
    {
        _stackedNotificationsBehavior.ShowWithWindowExtension(
            MicrosoftHyperVText,
            _stringResource.GetLocalized("RestartAfterChangesMessage"),
            InfoBarSeverity.Warning,
            RestartComputerCommand,
            _stringResource.GetLocalized("RestartButton"));
    }

    public void DisplayComputeSystemOperationError(string providerDisplayName, string computeSystemDisplayName, string errorText)
    {
        var titleText = $"{providerDisplayName} ({computeSystemDisplayName})";
        _stackedNotificationsBehavior.ShowWithWindowExtension(titleText, errorText, InfoBarSeverity.Error);
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
    private void AddUserToHyperVAdminGroupAndEnableHyperV(Notification notification)
    {
        var user = _windowsIdentityHelper.GetCurrentUserName();
        if (user == null)
        {
            _log.Error("Unable to get the current user name");
            return;
        }

        _stackedNotificationsBehavior.RemoveWithWindowExtension(notification);
        var startInfo = new ProcessStartInfo();

        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = $"powershell.exe";

        // Add the user to the Hyper-V Administrators group and enable the Hyper-V feature if it is not already enabled.
        // Using a string instead of a file for the script so it can't be manipulated via the file system.
        startInfo.Arguments = $"-ExecutionPolicy Bypass -Command \"{HyperVSetupScript.SetupFunction}\"";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;
        Task.Run(() =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception
            try
            {
                process.Start();
                process.WaitForExit();

                _stackedNotificationsBehavior.ClearWithWindowExtension();
                _log.Information($"Script exited with code: '{process.ExitCode}'");

                // ExitCodes come directly from within the script in HyperVSetupScript.SetupFunction.
                switch (process.ExitCode)
                {
                    case 0:
                        // The script successfully added the user to the Hyper-V Admin Group and enabled the Hyper-V Feature.
                        _shouldShowHyperVRebootButton = true;
                        ShowRestartNotification();
                        return;
                    case 2:
                        // Hyper-V Feature is already enabled and the script successfully added the user to the Hyper-V Admin group.
                        _shouldShowHyperVRebootButton = true;
                        ShowRestartNotification();
                        return;
                    case 3:
                        // Hyper-V Feature is already enabled and the script failed to add the user to the Hyper-V Admin group.
                        ShowErrorWithRebootAfterExecutionMessage(_stringResource.GetLocalized("UserNotAddedToHyperVAdminGroupMessage"));
                        return;
                    case 4:
                        // The user is already in the Hyper-V Admin group and the script successfully enabled the Hyper-Feature.
                        _shouldShowHyperVRebootButton = true;
                        ShowRestartNotification();
                        return;
                    case 5:
                        // The user is already in the Hyper-V Admin group and the script failed to enable the Hyper-Feature.
                        ShowErrorWithRebootAfterExecutionMessage(_stringResource.GetLocalized("UnableToEnableHyperVFeatureMessage"));
                        return;
                    case 6:
                        // Display nothing as there is no work to be done
                        return;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Script failed, we may not have been able to add user to Hyper-V admin group or enable Hyper-V");
            }

            ShowErrorWithRebootAfterExecutionMessage(_stringResource.GetLocalized("UnableToAddUserToHyperVAdminAndEnableHyperVMessage"));
        });
    }
}
