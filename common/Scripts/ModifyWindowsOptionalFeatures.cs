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

public class ModifyWindowsOptionalFeatures : IDisposable
{
    private readonly Process _process;
    private readonly ILogger? _log;
    private readonly CancellationToken _cancellationToken;
    private readonly string _featuresString;
    private Stopwatch _stopwatch = new();
    private bool _disposed;

    public ModifyWindowsOptionalFeatures(
        IEnumerable<WindowsOptionalFeatureState> features,
        ILogger? log,
        CancellationToken cancellationToken)
    {
        _log = log;
        _cancellationToken = cancellationToken;

        // Format the argument for the PowerShell script using `n as a newline character since the list
        // will be parsed with ConvertFrom-StringData.
        // The format is FeatureName1=True|False`nFeatureName2=True|False`n...
        _featuresString = string.Empty;
        foreach (var featureState in features)
        {
            if (featureState.HasChanged)
            {
                _featuresString += $"{featureState.Feature.FeatureName}={featureState.IsEnabled}`n";
            }
        }

        var scriptString = Script.Replace("FEATURE_STRING_INPUT", _featuresString);
        _process = new Process
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
    }

    public async Task<ExitCode> Execute()
    {
        var exitCode = ExitCode.Success;
        await Task.Run(
            () =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception,
            // which is handled as a failure.
            try
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _log?.Information("Operation was cancelled.");
                    exitCode = ExitCode.Cancelled;
                }

                _process.Start();
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt.
                exitCode = HandleProcessExecutionException(ex, _log);
            }
        },
            _cancellationToken);

        if (exitCode == ExitCode.Success)
        {
            _stopwatch = Stopwatch.StartNew();
        }

        return exitCode;
    }

    public async Task<ExitCode> WaitForCompleted()
    {
        var exitCode = ExitCode.Success;
        await Task.Run(
            () =>
        {
            try
            {
                while (!_process.WaitForExit(1000))
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        // Attempt to kill the process if cancellation is requested
                        exitCode = ExitCode.Cancelled;
                        _process.Kill();
                        _log?.Information("Operation was cancelled.");
                        return;
                    }
                }

                exitCode = FromExitCode(_process.ExitCode);
            }
            catch (Exception ex)
            {
                exitCode = HandleProcessExecutionException(ex, _log);
            }
        },
            _cancellationToken);

        _stopwatch.Stop();

        ModifyWindowsOptionalFeaturesEvent.Log(
            _featuresString,
            exitCode,
            _stopwatch.ElapsedMilliseconds);

        return ExitCode.Success;
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

    private static ExitCode HandleProcessExecutionException(Exception ex, ILogger? log = null)
    {
        if (ex is System.ComponentModel.Win32Exception win32Exception)
        {
            if (win32Exception.NativeErrorCode == 1223)
            {
                log?.Information(ex, "UAC was cancelled by the user.");
                return ExitCode.Cancelled;
            }
        }
        else
        {
            log?.Error(ex, "Script failed");
        }

        return ExitCode.Failure;
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

    protected void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _process.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// PowerShell script for modifying Windows optional features.
    ///
    /// This script takes a string argument representing feature names and their desired states (enabled or disabled).
    /// It parses this string into a dictionary, iterates over each feature, and performs the necessary enable or disable operation based on the desired state.
    ///
    /// The script defines the following possible exit statuses:
    /// - OperationSucceeded (0): All operations (enable or disable) succeeded.
    /// - OperationFailed (1): At least one operation failed.
    ///
    /// Only features present in "validFeatures" are considered valid. If an invalid feature name is encountered, the script exits with OperationFailed.
    /// This list should generally be kept consistent with the list of features in the WindowsOptionalFeatures class.
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
