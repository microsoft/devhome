// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using Serilog;

namespace DevHome.Common.Scripts;

public static class ModifyWindowsOptionalFeatures
{
    public static async Task ModifyFeaturesAsync(
        IEnumerable<OptionalFeatureState> features,
        OptionalFeatureNotificationHelper? notificationsHelper = null,
        ILogger? log = null)
    {
        if (!features.Any(f => f.HasChanged))
        {
            return;
        }

        var featuresString = string.Empty;

        foreach (var featureState in features)
        {
            if (featureState.HasChanged)
            {
                featuresString += $"{featureState.Feature.FeatureName}={featureState.IsEnabled}`n";
            }
        }

        var scriptString = Script.Replace("FEATURE_STRING_INPUT", featuresString);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command {scriptString}",
                UseShellExecute = true,
                Verb = "runas",
            },
        };

        await Task.Run(() =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception,
            // which is handled as a failure.
            var exitCode = ExitCode.Failure;

            try
            {
                process.Start();
                process.WaitForExit();

                exitCode = FromExitCode(process.ExitCode);
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt.
                log?.Error(ex, "Script failed");
            }

            notificationsHelper?.HandleModifyFeatureResult(exitCode);

            return Task.CompletedTask;
        });
    }

    public enum ExitCode
    {
        Success = 0,
        NoChange = 1,
        Failure = 2,
    }

    private static ExitCode FromExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => ExitCode.Success,
            1 => ExitCode.NoChange,
            _ => ExitCode.Failure,
        };
    }

    /// <summary>
    /// PowerShell script for modifying Windows optional features.
    ///
    /// This script takes a string argument representing feature names and their desired states (enabled or disabled).
    /// It parses this string into a dictionary, iterates over each feature, and performs the necessary enable or disable operation based on the desired state.
    ///
    /// The script defines the following possible exit statuses:
    /// - OperationSucceeded (0): All operations (enable or disable) succeeded and no restart is needed.
    /// - OperationSkipped (1): No operations were performed because the current state of all features matched the desired state.
    /// - OperationFailed (2): At least one operation failed.
    ///
    /// Only features present in "validFeatures" are considered valid. If an invalid feature name is encountered, the script exits with OperationFailed.
    /// This list should be kept consistent with the list of features in the WindowsOptionalFeatureNames class.
    ///
    /// </summary>
    private const string Script =
@"
enum OperationStatus
{
    OperationSucceeded = 0
    OperationSkipped = 1
    OperationFailed = 2
}

$validFeatures = @(
    'Containers',
    'HostGuardian',
    'Microsoft-Hyper-V-All',
    'Microsoft-Hyper-V-Tools-All',
    'Microsoft-Hyper-V',
    'VirtualMachinePlatform',
    'HypervisorPlatform',
    'Containers-DisposableClientVM',
    'Microsoft-Windows-Subsystem-Linux'
)

function ModifyFeatures($featuresString)
{
    $features = ConvertFrom-StringData $featuresString
    $anyOperationFailed = $false
    $anyOperationPerformed = $false

    foreach ($feature in $features.GetEnumerator())
    {
        $featureName = $feature.Key
        if ($featureName -notin $validFeatures)
        {
            exit [OperationStatus]::OperationFailed
        }

        $isEnabled = [bool]::Parse($feature.Value);
        $featureState = Get-WindowsOptionalFeature -FeatureName $featureName -Online | Select-Object -ExpandProperty State;
        $currentEnabled = $featureState -eq 'Enabled';

        if ($currentEnabled -ne $isEnabled)
        {
            $anyOperationPerformed = $true
            if ($isEnabled)
            {
                $enableResult = Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -NoRestart
                if ($enableResult -eq $null)
                {
                    $anyOperationFailed = $true
                }
            }
            else
            {
                $disableResult = Disable-WindowsOptionalFeature -Online -FeatureName $featureName -NoRestart
                if ($disableResult -eq $null)
                {
                    $anyOperationFailed = $true
                }
            }
        }
    }

    if ($anyOperationFailed)
    {
        exit [OperationStatus]::OperationFailed;
    }
    elseif ($anyOperationPerformed)
    {
        exit [OperationStatus]::OperationSucceeded;
    }
    {
        exit [OperationStatus]::OperationSkipped;
    }
}

ModifyFeatures FEATURE_STRING_INPUT;
";
}
