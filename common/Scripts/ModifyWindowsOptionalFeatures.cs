// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Common.Scripts;

public static class ModifyWindowsOptionalFeatures
{
    public const string ModifyFunction = @"
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
