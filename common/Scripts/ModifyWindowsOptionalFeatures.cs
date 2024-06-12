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

        var startInfo = new ProcessStartInfo();

        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = $"powershell.exe";
        startInfo.Arguments = $"-ExecutionPolicy Bypass -Command \"{Script.Replace("$args[0]", $"\"{featuresString}\"")}\"";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;
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
        SuccessRestartNeeded = 1,
        NoChange = 2,
        Failure = 3,
    }

    private static ExitCode FromExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => ExitCode.Success,
            1 => ExitCode.SuccessRestartNeeded,
            2 => ExitCode.NoChange,
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
    /// - OperationSucceededRestartNeeded (1): All operations (enable or disable) succeeded and a restart is needed.
    /// - OperationSkipped (2): No operations were performed because the current state of all features matched the desired state.
    /// - OperationFailed (3): At least one operation failed.
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
    OperationSucceededRestartNeeded = 1
    OperationSkipped = 2
    OperationFailed = 3
}

$validFeatures = @(
    ""Containers"",
    ""HostGuardian"",
    ""Microsoft-Hyper-V-All"",
    ""Microsoft-Hyper-V-Tools-All"",
    ""Microsoft-Hyper-V"",
    ""VirtualMachinePlatform"",
    ""HypervisorPlatform"",
    ""Containers-DisposableClientVM"",
    ""Microsoft-Windows-Subsystem-Linux""
)

function ModifyFeatures($featuresString)
{
    $features = ConvertFrom-StringData $featuresString

    foreach ($feature in $features.GetEnumerator())
    {
        $featureName = $feature.Key
        if ($featureName -notin $validFeatures)
        {
            Write-Error ""Invalid feature name: $featureName""
            exit [OperationStatus]::OperationFailed
        }

        $isEnabled = [bool]::Parse($feature.Value);

        $featureState = Get-WindowsOptionalFeature -FeatureName $featureName -Online | Select-Object -ExpandProperty State;
        $currentEnabled = $featureState -eq 'Enabled';

        if ($currentEnabled -ne $isEnabled)
        {
            $operationPerformed = $true

            if ($isEnabled)
            {
                $enableResult = Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -NoRestart

                if ($enableResult -eq $null)
                {
                    $operationFailed = $true
                }
                elseif ($enableResult.RestartNeeded -eq $true)
                {
                    $restartNeeded = $true
                }
            }
            else
            {
                $disableResult = Disable-WindowsOptionalFeature -Online -FeatureName $featureName -NoRestart

                if ($disableResult -eq $null)
                {
                    $operationFailed = $true
                }
                elseif ($disableResult.RestartNeeded -eq $true)
                {
                    $restartNeeded = $true
                }
            }
        }
    }

    if ($operationFailed)
    {
        exit [OperationStatus]::OperationFailed;
    }
    elseif (-not $operationPerformed)
    {
        exit [OperationStatus]::OperationSkipped;
    }
    elseif ($restartNeeded)
    {
        exit [OperationStatus]::OperationSucceededRestartNeeded
    }
    else
    {
        exit [OperationStatus]::OperationSucceeded;
    }
}

ModifyFeatures $args[0];
";
}
