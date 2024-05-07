// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.Environments.Scripts;

public static class HyperVSetupScript
{
    public const string SetupFunction = @"
            
            # For cmdlet operation results so we can compare results and choose the right exit code
            enum OperationStatus
            {
                OperationSucceeded = 0
                OperationFailed = 1
                OperationNotRun = 2
            }

           <#
                This script does the following:
                1. Checks if the user is in the Hyper-V Administrators group and adds them if they are not in it
                2. Checks if the Hyper-V Feature is enabled and enables the feature if it is not already enabled.
                3. If the user is in the Hyper-V Administrators group and the Hyper-V feature is enabled. The script does nothing.

                The script uses the following exit codes to signal to Dev Home what message it should show to the user.

                Exit Codes (Note: we don't use exit code of 1 since that's the generic exit code if there was an exception):
                0. The script successfully added the user to the Hyper-V Admin Group and enabled the Hyper-V Feature.
                2. The user is not in the Hyper-V Admin Group and the Hyper-V Feature is already enabled. The script successfully added the user to the Hyper-V Admin group.
                3. The user is not in the Hyper-V Admin Group and the Hyper-V Feature is already enabled. The script failed to add the user to the Hyper-V Admin group.
                4. The user is in the Hyper-V Admin group and the Hyper-V Feature is not already enabled. The script successfully enabled the Hyper-Feature.
                5. The user is in the Hyper-V Admin group and the Hyper-V Feature is not already enabled. The script failed to enable the Hyper-Feature.
                6. The user is already in the Hyper-V Admin group and the Hyper-V Feature is already enabled.
            #>
        function Initialize-HyperVForDevHome()
        {
            $featureEnablementResult = [OperationStatus]::OperationNotRun
            $adminGroupResult = [OperationStatus]::OperationNotRun

            # Check the security token the user logged on with contains the Hyper-V Administrators group SID (S-1-5-32-578). This can only be updated,
            # once the user logs off and on again. Even if we add the user to the group later on in the script.
            $foundSecurityTokenString = [System.Security.Principal.WindowsIdentity]::GetCurrent().Groups.Value | Where-Object { $_ -eq 'S-1-5-32-578' }
            $doesUserSecurityTokenContainHyperAdminGroup = $foundSecurityTokenString -eq 'S-1-5-32-578'

            # Check if the Hyper-V feature is enabled
            $featureState = Get-WindowsOptionalFeature -FeatureName 'Microsoft-Hyper-V' -Online | Select-Object -ExpandProperty State
            $featureEnabled = $featureState -eq 'Enabled'

            if ($doesUserSecurityTokenContainHyperAdminGroup -and $featureEnabled)
            {
                # User already in Admin group and feature already enabled
                exit 6
            }

            # Enable the Hyper-V feature if it is not already enabled
            if (-not $featureEnabled)
            {
                $dsimHyperVFeature = Enable-WindowsOptionalFeature -Online -FeatureName 'Microsoft-Hyper-V' -All -NoRestart
                
                # when $dsimHyperVFeature is not null we've enabled the feature successfully
                if ($null -ne $dsimHyperVFeature)
                {
                    # Hyper-V feature enabled successfully.
                    $featureEnablementResult = [OperationStatus]::OperationSucceeded 
                }
                else
                {    
                    # Failed to enable the Hyper-V feature.
                    $featureEnablementResult = [OperationStatus]::OperationFailed 
                }
            }

            # Check the Hyper-V Administrators group to see if the user is inside the group
            $userGroupObject = Get-LocalGroupMember -Group 'Hyper-V Administrators' | Where-Object { $_.Name -eq ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name) }
            $isUserInGroup = $null -ne $userGroupObject
            
            # Add user to Hyper-v Administrators group if they aren't already in the group
            if (-not $isUserInGroup) 
            {
                Add-LocalGroupMember -Group 'Hyper-V Administrators' -Member ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)

                # Check if the last command succeeded
                if ($?)
                {
                    # User added to the Hyper-V Administrators group.
                    $adminGroupResult = [OperationStatus]::OperationSucceeded 
                }
                else
                {
                    # Failed to add user to the Hyper-V Administrators group.
                    $adminGroupResult = [OperationStatus]::OperationFailed 
                }
            }

            # We added the user to the admin group and enabled the Hyper-V feature during this script
            if ($adminGroupResult -eq [OperationStatus]::OperationSucceeded -and $featureEnablementResult -eq [OperationStatus]::OperationSucceeded)
            {
                exit 0
            }
            # We added the user to the admin group but the Hyper-V feature was already enabled before this script ran
            elseif ($adminGroupResult -eq [OperationStatus]::OperationSucceeded -and $featureEnablementResult -eq [OperationStatus]::OperationNotRun)
            {
                exit 2
            }
            # We failed to add the user to the admin group and the Hyper-V feature was already enabled before this script ran
            elseif ($adminGroupResult -eq [OperationStatus]::OperationFailed -and $featureEnablementResult -eq [OperationStatus]::OperationNotRun)
            {
                exit 3
            }
            # We enabled the Hyper-V feature but the user was already in the Hyper-V admin group before this script ran
            elseif ($featureEnablementResult -eq [OperationStatus]::OperationSucceeded -and $adminGroupResult -eq [OperationStatus]::OperationNotRun)
            {
                exit 4
            }
            # We failed to enable the Hyper-V feature and the user was already in the Hyper-V admin group before this script ran
            elseif ($featureEnablementResult -eq [OperationStatus]::OperationFailed -and $adminGroupResult -eq [OperationStatus]::OperationNotRun)
            {
                exit 5
            }
            # If both operations have not been run at this point, then user is already in the Hyper-V admin group and the Hyper-V feature is enabled.
            # This could happen if the script runs the first time without the user being in the group, while Hyper-V is enabled but the user doesn't
            # log off/on again or reboot. The second time we run the script there would be no work to be done. Since the actual token of the user
            # doesn't update until they log off, the $doesUserSecurityTokenContainHyperAdminGroup variable above will still remain false, which is
            # how we ended up here.
            elseif ($featureEnablementResult -eq [OperationStatus]::OperationNotRun -and $adminGroupResult -eq [OperationStatus]::OperationNotRun)
            {
                exit 6
            }

            # If we get here we instruct the user to check both manually. This could happen if one failed and the other succeeded or if both failed.
            exit 99
        }

        # Run script
        Initialize-HyperVForDevHome
        ";
}
