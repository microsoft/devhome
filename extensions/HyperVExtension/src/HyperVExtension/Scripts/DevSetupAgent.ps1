<#
.SYNOPSIS
 
Install DevSetupAgent Windows service and DevSetupEngine COM server on a VM
 
.DESCRIPTION
 
Install DevSetupAgent Windows service and DevSetupEngine COM server on a VM
through the provided PSSession.
#>

function Install-DevSetupAgent
{
    Param(
        [Parameter(Mandatory = $true)]
        [Guid] $VMId,
        
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.Runspaces.PSSession] $Session,
        
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

        $ErrorActionPreference = "Stop"
        $activity = "Installing DevSetupAgent to VM $VMId"

        # Validate input. Only .cab and .zip files are supported
        # If $Path is a directory, it will be copied to the VM and installed as is
        $isDirectory = $false
        $isCab = $false
        $inputFileName = $null
        if (Test-Path -Path $Path -PathType 'Container')
        {
            $isDirectory = $true
        }
        elseif (Test-Path -Path $Path -PathType 'Leaf')
        {
            if ($Path -match '\.(cab)$')
            {
                $isCab = $true
            }
            elseif (-not $Path -match '\.(zip)$')
            {
                throw "Only .cab and .zip files are supported"
            }
            $inputFileName = Split-Path -Path $Path -Leaf
        }
        else
        {
            throw "$Path does not exist"
        }


        $DevSetupAgentConst = "DevSetupAgent"
        $DevSetupEngineConst = "DevSetupEngine"
        $session = $Session

        $guestTempDirectory = Invoke-Command -Session $session -ScriptBlock { $env:temp }
        
        [string] $guid = [System.Guid]::NewGuid()
        $guestUnpackDirectory = Join-Path -Path $guestTempDirectory -ChildPath $guid
        $guestDevSetupAgentTempDirectory = Join-Path -Path $guestUnpackDirectory -ChildPath $DevSetupAgentConst

        Write-Host "Creating VM temporary folder $guestUnpackDirectory"
        Write-Progress -Activity $activity -Status "Creating VM temporary folder $guestUnpackDirectory" -PercentComplete 10
        Invoke-Command -Session $session -ScriptBlock { New-Item -Path "$using:guestUnpackDirectory" -ItemType "directory" }

        if ($isDirectory)
        {
            $destinationPath = $guestDevSetupAgentTempDirectory
        }
        else
        {
            $destinationPath = $guestUnpackDirectory
        }

        Write-Host "Copying $Path to VM $destinationPath"
        Write-Progress -Activity $activity -Status "Copying DevSetupAgent to VM $destinationPath" -PercentComplete 15
        Copy-Item -ToSession $session -Recurse -Path $Path -Destination $destinationPath


        Invoke-Command -Session $session -ScriptBlock {
            $ErrorActionPreference = "Stop"

            try
            {
                $guestDevSetupAgentPath = Join-Path -Path $Env:Programfiles -ChildPath $using:DevSetupAgentConst

                # Stop and remove previous version of DevSetupAgent service if it exists
                $service = Get-Service -Name $using:DevSetupAgentConst -ErrorAction SilentlyContinue
                if ($service)
                {
                    $serviceWMI = Get-WmiObject -Class Win32_Service -Filter "Name='$using:DevSetupAgentConst'"
                    $existingServicePath = $serviceWMI.Properties["PathName"].Value
                    if ($existingServicePath)
                    {
                        $guestDevSetupAgentPath = Split-Path $existingServicePath -Parent
                    }
                    
                    try
                    {
                        Write-Host "Stopping DevSetupAgent service"
                        Write-Progress -Activity $using:activity -Status "Stopping DevSetupAgent service $destinationPath" -PercentComplete 30
                        $service.Stop()
                    }
                    catch
                    {
                        Write-Host "Ignoring error: $PSItem"
                    }

                    Remove-Variable -Name service -ErrorAction SilentlyContinue

                    # Remove-Service is only available in PowerShell 6.0 and later. Windows doesn't come with it preinstalled.
                    Write-Host "Removing DevSetupAgent service"
                    Write-Progress -Activity $using:activity -Status "Removing DevSetupAgent service" -PercentComplete 35
                    $serviceWMI = Get-WmiObject -Class Win32_Service -Filter "Name='$using:DevSetupAgentConst'"
                    $serviceWMI.Delete()
                    Remove-Variable -Name serviceWMI -ErrorAction SilentlyContinue
                }

                # Stop previous version of DevSetupEngine COM server if it exists
                $devSetupEngineProcess = Get-Process -Name "$using:DevSetupEngineConst" -ErrorAction SilentlyContinue
                if ($devSetupEngineProcess -ne $null)
                {
                    Write-Host "Stopping $using:DevSetupEngineConst process"
                    Write-Progress -Activity $using:activity -Status "Stopping $using:DevSetupEngineConst process" -PercentComplete 40
                    Stop-Process -Force -Name "$using:DevSetupEngineConst"
                }

                # Unregister DevSetupEngine
                $enginePath = Join-Path -Path $guestDevSetupAgentPath -ChildPath "$using:DevSetupEngineConst.exe"
                if (Test-Path -Path $enginePath)
                {
                    Write-Host "Unregistering DevSetupEngine ($enginePath)"
                    Write-Progress -Activity $using:activity -Status "Registering DevSetupEngine ($enginePath)" -PercentComplete 88
                    &$enginePath "-UnregisterComServer"
                }
            
                # Remove previous version of DevSetupAgent service files
                if (Test-Path -Path $guestDevSetupAgentPath)
                {
                    # Sleep a few seconds to make sure all handles released after shutting down previous DevSetupEngine
                    Start-Sleep -Seconds 7
                    Write-Host "Deleting old DevSetupAgent service files"
                    Write-Progress -Activity $using:activity -Status "Deleting old DevSetupAgent service files" -PercentComplete 45
                    Remove-Item -Recurse -Force -Path $guestDevSetupAgentPath
                }

                if ($using:isDirectory)
                {
                    Write-Host "Copying DevSetupAgent to $guestDevSetupAgentPath"
                    Write-Progress -Activity $using:activity -Status "Deleting old DevSetupAgent service files" -PercentComplete 50
                    Copy-Item -Recurse -Path $using:guestDevSetupAgentTempDirectory -Destination $guestDevSetupAgentPath
                }
                elseif ($using:isCab)
                {
                    $cabPath = Join-Path -Path $using:guestUnpackDirectory -ChildPath $using:inputFileName
                    Write-Host "Unpacking $cabPath to $guestDevSetupAgentPath"
                    Write-Progress -Activity $using:activity -Status "Unpacking $cabPath to $guestDevSetupAgentPath" -PercentComplete 60
                    $expandOutput=&"$Env:SystemRoot\System32\expand.exe" $cabPath /F:* $Env:Programfiles
                    if ($LastExitCode -ne 0)
                    {
                        throw "Error unpacking $cabPath`:`n$LastExitCode`n$($expandOutput|Out-String)"
                    }
                }
                else
                {
                    $zipPath = Join-Path -Path $using:guestUnpackDirectory -ChildPath $using:inputFileName
                    Write-Host "Unpacking $using:inputFileName to $guestDevSetupAgentPath"
                    Write-Progress -Activity $using:activity -Status "Unpacking $using:inputFileName to $guestDevSetupAgentPath" -PercentComplete 60
                    Expand-Archive -Path $zipPath -Destination $guestDevSetupAgentPath
                }

                # Register DevSetupAgent service
                $servicePath = Join-Path -Path $guestDevSetupAgentPath -ChildPath "$using:DevSetupAgentConst.exe"
                Write-Host "Registering DevSetupAgent service ($servicePath)"
                Write-Progress -Activity $using:activity -Status "Registering DevSetupAgent service ($servicePath)" -PercentComplete 85
                New-Service -Name $using:DevSetupAgentConst -BinaryPathName $servicePath -StartupType Automatic

                # Register DevSetupEngine
                Write-Host "Registering DevSetupEngine ($enginePath)"
                Write-Progress -Activity $using:activity -Status "Registering DevSetupEngine ($enginePath)" -PercentComplete 88

                # Executing non-console apps using '&' does not set $LastExitCode. Using Start-Process here to get the returned error code.
                $process = Start-Process -NoNewWindow -Wait $enginePath -ArgumentList "-RegisterComServer" -PassThru
                if ($process.ExitCode -ne 0)
                {
                    throw "Error registering $enginePath`: $process.ExitCode"
                }

                Write-Host "Starting DevSetupAgent service"
                Write-Progress -Activity $using:activity -Status "Starting DevSetupAgent service" -PercentComplete 92
                Start-Service $using:DevSetupAgentConst
            }
            catch
            {
                Write-Host "Error on guest OS: $PSItem"
            }
            finally
            {
                Write-Host "Removing temporary directory $using:guestUnpackDirectory"
                Remove-Item -Recurse -Force -Path $using:guestUnpackDirectory -ErrorAction SilentlyContinue 
            }
        }

        Remove-PSSession $session
}
