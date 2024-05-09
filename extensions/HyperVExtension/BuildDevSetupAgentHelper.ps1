Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [string]$VersionOfSDK,
    [string]$SDKNugetSource,
    [string]$Version,
    [string]$BuildStep = "all",
    [string]$AzureBuildingBranch = "main",
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

  -Platform <platform>
      Only build the selected platform(s)
      Example: -Platform x64
      Example: -Platform "x86,x64,arm64"

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
This script is not meant to be run directly. To build DevSetupAgent, please run the following from the root directory:
build -BuildStep "DevSetupAgent"
"@ -ForegroundColor RED
  Exit
}

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')

$ErrorActionPreference = "Stop"

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
if ($IsAzurePipelineBuild) {
  $nugetPath = "nuget.exe";
} else {
  $nugetPath = (Join-Path $env:Build_RootDirectory "build\NugetWrapper.cmd")
}

if (-not([string]::IsNullOrWhiteSpace($SDKNugetSource))) {
  & $nugetPath sources add -Source $SDKNugetSource
}

Try {
  $buildRing = "Dev"

  if ($AzureBuildingBranch -ieq "release") {
    $buildRing = "Stable"
  } elseif ($AzureBuildingBranch -ieq "staging") {
    $buildRing = "Canary"
  }

  Write-Host $nugetPath

  & $nugetPath restore

  $msbuildArgs = @(
      ("extensions\HyperVExtension\DevSetupAgent.sln"),
      ("/m"),
      ("/p:Platform="+$platform),
      ("/p:Configuration="+$configuration),
      ("/restore"),
      ("/binaryLogger:DevSetupAgent.$platform.$configuration.binlog"),
      ("/p:BuildRing=$buildRing")
  )
  if (-not([string]::IsNullOrWhiteSpace($VersionOfSDK))) {
    $msbuildArgs += ("/p:DevHomeSDKVersion="+$env:sdk_version)
  }

  & $msbuildPath $msbuildArgs

# SDK version and .NEt version needs to stay in sync with ToolingVersion.props, DevSetupEngineIdl.vcxproj, and DevHome-CL.yaml
  $binariesOutputPath = (Join-Path $env:Build_RootDirectory "extensions\HyperVExtension\src\DevSetupAgent\bin\$Platform\$Configuration\net8.0-windows10.0.22621.0\win-$Platform\*")
  $zipOutputPath = (Join-Path $env:Build_RootDirectory "extensions\HyperVExtension\src\DevSetupAgent\bin\$Platform\$Configuration\DevSetupAgent_$Platform.zip")

  Compress-Archive -Force -Path $binariesOutputPath $zipOutputPath
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
