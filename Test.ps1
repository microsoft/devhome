param (
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$RunUITests = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      Test.cmd [options]

Description:
      Runs Dev Home tests.

Options:

  -Platform <platform>
      Only build the selected platform(s)
      Example: -Platform x64
      Example: -Platform "x86,x64,arm64"

  -Configuration <configuration>
      Only build the selected configuration(s)
      Example: -Configuration release
      Example: -Configuration "debug,release"

  -Help
      Display this usage message.
"@
    Exit
}

$env:Build_SourcesDirectory = (Split-Path $MyInvocation.MyCommand.Path)
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()

$vstestPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -find '**\TestPlatform\vstest.console.exe'

$ErrorActionPreference = "Stop"

$isInstalled = Get-ItemProperty -Path HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\* | Where-Object { $_.DisplayName -like "*Windows Application Driver*" }

if (-not $IsAzurePipelineBuild) {
    if ($isInstalled) {
        Write-Host "WinAppDriver is already installed on this computer."

        Start-Process -FilePath "C:\Program Files\Windows Application Driver\WinAppDriver.exe"
    } else {
        Write-Host "WinAppDriver will be installed in the background."
        $url = "https://github.com/microsoft/WinAppDriver/releases/download/v1.2.99/WindowsApplicationDriver-1.2.99-win-x64.exe"
        $outpath = Join-Path $env:Build_SourcesDirectory "temp"
        
        if (-not (Test-Path -Path $outpath)) {
            New-Item -ItemType Directory -Path $outpath | Out-Null
        }
        
        Invoke-WebRequest -Uri $url -OutFile (Join-Path $outpath "WinAppDriverx64.exe")

        Start-Process -Wait -FilePath (Join-Path $env:Build_SourcesDirectory "temp\WinAppDriverx64.exe") -ArgumentList "/S" -PassThru
    }
}

function ShutDownTests {
    if (-not $IsAzurePipelineBuild) {
        Stop-Process -Name "WinAppDriver" -ErrorAction SilentlyContinue
    }

    $TotalTime = (Get-Date) - $StartTime
    $TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
    $TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

    Write-Host @"
    Total Running Time:
    $TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor Cyan
}

if (-not (Test-Path -Path "AppxPackages")) {
    Write-Host "Nothing to test. Ensure you have built via the command line before running tests. Exiting." -ForegroundColor Yellow
    Exit 1
}

try {
    foreach ($platform in $env:Build_Platform.Split(",")) {
        foreach ($configuration in $env:Build_Configuration.Split(",")) {
            # TODO: UI tests are currently disabled in the pipeline until signing is solved
            if ($RunUITests) {
                $DevHomePackage = Get-AppPackage "Microsoft.DevHome" -ErrorAction SilentlyContinue
                if ($DevHomePackage) {
                    Write-Host "Uninstalling old Dev Home"
                    Remove-AppPackage -Package $DevHomePackage.PackageFullName
                }
                Write-Host "Installing Dev Home"
                Add-AppPackage (Join-Path "AppxPackages" "$configuration\DevHome-$platform.msix")

                if ($true) {
                    # Start/stop the app once so that WinAppDriver doesn't time out during first time setup
                    # and wait 60 seconds to give plenty of time
                    Start-Process "Shell:AppsFolder\Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App"
                    Start-Sleep 60
                    Stop-Process -Name "DevHome"
                }
            }

            $vstestArgs = @(
                "/Platform:$platform",
                "/Logger:trx;LogFileName=DevHome.Test-$platform-$configuration.trx",
                "test\bin\$platform\$configuration\net8.0-windows10.0.22621.0\DevHome.Test.dll"
            )
            $winAppTestArgs = @(
                "/Platform:$platform",
                "/Logger:trx;LogFileName=DevHome.UITest-$platform-$configuration.trx",
                "/Settings:uitest\Test.runsettings",
                "uitest\bin\$platform\$configuration\net8.0-windows10.0.22621.0\DevHome.UITest.dll"
            )

            & $vstestPath $vstestArgs
            # TODO: UI tests are currently disabled in the pipeline until signing is solved
            if ($RunUITests) {
                & $vstestPath $winAppTestArgs
            }

            foreach ($toolPath in (Get-ChildItem "tools")) {
                $tool = $toolPath.Name
                $vstestArgs = @(
                    "/Platform:$platform",
                    "/Logger:trx;LogFileName=$tool.Test-$platform-$configuration.trx",
                    "tools\$tool\*UnitTest\bin\$platform\$configuration\net8.0-windows10.0.22621.0\*.UnitTest.dll"
                )

                $winAppTestArgs = @(
                    "/Platform:$platform",
                    "/Logger:trx;LogFileName=$tool.UITest-$platform-$configuration.trx",
                    "/Settings:uitest\Test.runsettings",
                    "tools\$tool\*UITest\bin\$platform\$configuration\net8.0-windows10.0.22621.0\*.UITest.dll"
                )

                & $vstestPath $vstestArgs
                # TODO: UI tests are currently disabled in the pipeline until signing is solved
                if ($RunUITests) {
                    & $vstestPath $winAppTestArgs
                }
            }
        }
    }
} catch {
    $formatString = "`n{0}`n`n{1}`n`n"
    $fields = $_, $_.ScriptStackTrace
    Write-Host ($formatString -f $fields) -ForegroundColor Red
    ShutDownTests
    Exit 1
}

ShutDownTests
