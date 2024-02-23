Param(
  [string]$Configuration = "release",
  [bool]$IsAzurePipelineBuild = $false,
  [switch]$BypassWarning = $false,
  [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
  Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      BuildDevSetupAgentHelper.cmd [options]

Description:
      Builds DevSetupAgent.

Options:

  -Configuration <configuration>
      Only build the selected configuration(s)
      Example: -Configuration Release
      Example: -Configuration "Debug,Release"

  -Help
      Display this usage message.
"@
  Exit
}

if (-not $BypassWarning) {
  Write-Host @"
This script is not meant to be run directly.  To build DevSetupAgent, please run the following from the root directory:
build -BuildStep "DevSetupAgent"
"@ -ForegroundColor RED
  Exit
}

$ErrorActionPreference = "Stop"

$buildPlatforms = "x64","x86","arm64","AnyCPU"

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
if ($IsAzurePipelineBuild) {
  $nugetPath = "nuget.exe";
} else {
  $nugetPath = (Join-Path $PSScriptRoot "..\build\NugetWrapper.cmd")
}

New-Item -ItemType Directory -Force -Path "$PSScriptRoot\_build"
& $nugetPath restore (Join-Path $PSScriptRoot "src\DevSetupAgent\DevSetupAgent.csproj")

Try {
  foreach ($platform in $buildPlatforms) {
    foreach ($config in $Configuration.Split(",")) {
      $msbuildArgs = @(
        ("$PSScriptRoot\src\DevSetupAgent\DevSetupAgent.csproj"),
        ("/p:Platform="+$platform),
        ("/p:Configuration="+$config),
        ("/binaryLogger:DevHome.HyperV.DevSetupAgent.$platform.$config.binlog")
      )

      & $msbuildPath $msbuildArgs
    }
  }
} Catch {
  $formatString = "`n{0}`n`n{1}`n`n"
  $fields = $_, $_.ScriptStackTrace
  Write-Host ($formatString -f $fields) -ForegroundColor RED
  Exit 1
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

Write-Host @"
Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN