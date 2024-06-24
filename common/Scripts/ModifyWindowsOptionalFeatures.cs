// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.TelemetryEvents;
using Serilog;

namespace DevHome.Common.Scripts;

public static class ModifyWindowsOptionalFeatures
{
    public static async Task<ExitCode> ModifyFeaturesAsync(
        IEnumerable<WindowsOptionalFeatureState> features,
        ILogger? log = null,
        CancellationToken cancellationToken = default)
    {
        if (!features.Any(f => f.HasChanged))
        {
            return ExitCode.Success;
        }

        // Format the argument for the PowerShell script using `n as a newline character since the list
        // will be parsed with ConvertFrom-StringData.
        // The format is FeatureName1=True|False`nFeatureName2=True|False`n...
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

        var exitCode = ExitCode.Failure;

        Stopwatch stopwatch = Stopwatch.StartNew();

        await Task.Run(
            () =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception,
            // which is handled as a failure.
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    log?.Information("Operation was cancelled.");
                    exitCode = ExitCode.Cancelled;
                    return;
                }

                process.Start();
                while (!process.WaitForExit(1000))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        // Attempt to kill the process if cancellation is requested
                        exitCode = ExitCode.Cancelled;
                        process.Kill();
                        log?.Information("Operation was cancelled.");
                        return;
                    }
                }

                exitCode = FromExitCode(process.ExitCode);
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt.
                if (ex is System.ComponentModel.Win32Exception win32Exception)
                {
                    if (win32Exception.NativeErrorCode == 1223)
                    {
                        log?.Information(ex, "UAC was cancelled by the user.");
                        exitCode = ExitCode.Cancelled;
                    }
                }
                else
                {
                    log?.Error(ex, "Script failed");
                    exitCode = ExitCode.Failure;
                }
            }
        },
            cancellationToken);

        stopwatch.Stop();

        ModifyWindowsOptionalFeaturesEvent.Log(
            featuresString,
            exitCode,
            stopwatch.ElapsedMilliseconds);

        return exitCode;
    }

    public enum ExitCode
    {
        Success = 0,
        Failure = 1,
        Cancelled = 2,
    }

    private static ExitCode FromExitCode(int exitCode)
    {
        return exitCode switch
        {
            0 => ExitCode.Success,
            1 => ExitCode.Failure,
            _ => ExitCode.Cancelled,
        };
    }

    public static string GetExitCodeDescription(ExitCode exitCode)
    {
        return exitCode switch
        {
            ExitCode.Success => "Success",
            ExitCode.Failure => "Failure",
            _ => "Cancelled",
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
$ErrorActionPreference='stop'

enum OperationStatus
{
    OperationSucceeded = 0
    OperationFailed = 1
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
            $result = $null
            if ($isEnabled)
            {
                $result = Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -NoRestart
            }
            else
            {
                $result = Disable-WindowsOptionalFeature -Online -FeatureName $featureName -NoRestart
            }

            if ($null -eq $result)
            {
                exit [OperationStatus]::OperationFailed;
            }
        }
    }

    exit [OperationStatus]::OperationSucceeded;
}

ModifyFeatures FEATURE_STRING_INPUT;
";
}
