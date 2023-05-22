Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation and Contributors.
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

$vstestPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -find **\TestPlatform\vstest.console.exe

$ErrorActionPreference = "Stop"

$isInstalled = Get-ChildItem HKLM:\SOFTWARE\$_\Microsoft\Windows\CurrentVersion\Uninstall\ | ? {($_.GetValue("DisplayName")) -like "*Windows Application Driver*"}

if (-not($IsAzurePipelineBuild)) {
  if ($isInstalled){
    Write-Host "WinAppDriver is already installed on this computer."
  }
  else {
    Write-Host "WinAppDriver will be installed in the background."
    $url = "https://github.com/microsoft/WinAppDriver/releases/download/v1.2.99/WindowsApplicationDriver-1.2.99-win-x64.exe"
    $outpath = "$env:Build_SourcesDirectory\temp"
    if (-not(Test-Path -Path $outpath)) {
        New-Item -ItemType Directory -Path $outpath | Out-Null
    }
    Invoke-WebRequest -Uri $url -OutFile "$env:Build_SourcesDirectory\temp\WinAppDriverx64.exe"

    Start-Process -Wait -Filepath $env:Build_SourcesDirectory\temp\WinAppDriverx64.exe -ArgumentList "/S" -PassThru
  }

  Start-Process -FilePath "C:\Program Files\Windows Application Driver\WinAppDriver.exe" 
}

Function ShutDownTests {
  if (-not($IsAzurePipelineBuild)) {
    Stop-Process -Name "WinAppDriver"
  }

  $TotalTime = (Get-Date)-$StartTime
  $TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
  $TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

  Write-Host @"

  Total Running Time:
  $TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN
}

if (-not(Test-Path -Path "AppxPackages")) {
  Write-Host "Nothing to test. Ensure you have built via the command line before running tests. Exiting." -ForegroundColor YELLOW
  Exit 1
}

Try {
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      # TODO: UI tests are currently disabled in pipeline until signing is solved
      if (-not($IsAzurePipelineBuild)) {
        $DevHomePackage = Get-AppPackage "Microsoft.DevHome"
        if ($DevHomePackage) {
          Write-Host "Uninstalling old Dev Home"
          Remove-AppPackage -Package $DevHomePackage.PackageFullName
        }
        Write-Host "Installing Dev Home"
        Add-AppPackage "AppxPackages\$configuration\DevHome-$platform.msix"
      }

      $vstestArgs = @(
          ("/Platform:$platform"),
          ("/Logger:trx;LogFileName=DevHome.Test-$platform-$configuration.trx"),
          ("test\bin\$platform\$configuration\net6.0-windows10.0.22000.0\DevHome.Test.dll")
      )
      $winAppTestArgs = @(
          ("/Platform:$platform"),
          ("/Logger:trx;LogFileName=DevHome.UITest-$platform-$configuration.trx"),
          ("uitest\bin\$platform\$configuration\net6.0-windows10.0.22000.0\DevHome.UITest.dll")
      )

      & $vstestPath $vstestArgs
      # TODO: UI tests are currently disabled in pipeline until signing is solved
      if (-not($IsAzurePipelineBuild)) {
          & $vstestPath $winAppTestArgs
      }

      foreach ($toolPath in (Get-ChildItem "tools")) {
        $tool = $toolPath.Name
        $vstestArgs = @(
            ("/Platform:$platform"),
            ("/Logger:trx;LogFileName=$tool.Test-$platform-$configuration.trx"),
            ("tools\$tool\*UnitTest\bin\$platform\$configuration\net6.0-windows10.0.22000.0\*.UnitTest.dll")
        )

        $winAppTestArgs = @(
            ("/Platform:$platform"),
            ("/Logger:trx;LogFileName=$tool.UITest-$platform-$configuration.trx"),
            ("tools\$tool\*UITest\bin\$platform\$configuration\net6.0-windows10.0.22000.0\*.UITest.dll")
        )

        & $vstestPath $vstestArgs
        # TODO: UI tests are currently disabled in pipeline until signing is solved
        if (-not($IsAzurePipelineBuild)) {
          & $vstestPath $winAppTestArgs
        }
      }
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  ShutDownTests
  Exit 1
}

ShutDownTests
