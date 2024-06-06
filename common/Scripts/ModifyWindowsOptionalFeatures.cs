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
using WinRT;

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

        var featureStrings = new List<string>();

        foreach (var featureState in features)
        {
            if (featureState.HasChanged)
            {
                featureStrings.Add($"{featureState.Feature.FeatureName}={featureState.IsEnabled}");
            }
        }

        var featureStringsArray = featureStrings.ToArray();

        var startInfo = new ProcessStartInfo();

        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = $"DevHome.Elevation.ZoneLaunchPad.exe";
        startInfo.Arguments = $"-ElevationZone VirtualMachineManagement -VoucherName vmmInstance";
        startInfo.UseShellExecute = true;
        startInfo.Verb = "runas";

        var process = new Process();
        process.StartInfo = startInfo;
        await Task.Run(() =>
        {
            // Since a UAC prompt will be shown, we need to wait for the process to exit
            // This can also be cancelled by the user which will result in an exception,
            // which is handled as a failure.
            try
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException();
                }

                // Acquire elevation voucher and redeem for the 'elevation zone'
                var elevationVoucher = DevHome.Elevation.ElevationVoucherManager.ClaimVoucher("vmmInstance");
                var zoneInterface = elevationVoucher.Redeem();
                var zone = zoneInterface.As<DevHome.Elevation.Zones.VirtualMachineManagement>();

                // Set the Windows Optional Features
                var status = zone.ModifyFeatures(featureStringsArray);

                // TODO: Get status object from zone and handle the result
                notificationsHelper?.HandleModifyFeatureResult(status);
            }
            catch (Exception ex)
            {
                // This is most likely a case where the user cancelled the UAC prompt.
                log?.Error(ex, "Script failed");
            }

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

    private const string Script = @"
        enum OperationStatus
        {
            OperationSucceeded = 0
            OperationSkipped = 1
            OperationFailed = 2
        }

        function InitializeFeatures($featuresString)
        {
            $features = ConvertFrom-StringData $featuresString;

            $exitCode = [OperationStatus]::OperationSkipped;

            foreach ($feature in $features.GetEnumerator())
            {
                $featureName = $feature.Key;
                $isEnabled = [bool]::Parse($feature.Value);

                $featureState = Get-WindowsOptionalFeature -FeatureName $featureName -Online | Select-Object -ExpandProperty State;
                $currentEnabled = $featureState -eq 'Enabled';

                if ($currentEnabled -ne $isEnabled)
                {
                    if ($isEnabled)
                    {
                        $enableResult = Enable-WindowsOptionalFeature -Online -FeatureName $featureName -All -NoRestart;

                        if ($enableResult -eq $null)
                        {
                            $exitCode = [OperationStatus]::OperationFailed;
                        }
                        else
                        {
                            $exitCode = [OperationStatus]::OperationSucceeded;
                        }
                    }
                    else
                    {
                        $disableResult = Disable-WindowsOptionalFeature -Online -FeatureName $featureName -NoRestart;

                        if ($disableResult -eq $null)
                        {
                            $exitCode = [OperationStatus]::OperationFailed;
                        }
                        else
                        {
                            $exitCode = [OperationStatus]::OperationSucceeded;
                        }
                    }
                }
            }

            exit $exitCode;
        }

        InitializeFeatures $args[0];
    ";
}
