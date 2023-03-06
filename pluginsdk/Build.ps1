Param(
  [string]$Configuration = "Release",
  [string]$SDKVersion,
  [bool]$IsAzurePipelineBuild = $false,
  [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
  Write-Host @"
Copyright (c) Microsoft Corporation and Contributors.
Licensed under the MIT License.

Syntax:
      Build.cmd [options]

Description:
      Builds Dev Home SDK.

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

$ErrorActionPreference = "Stop"

$buildPlatforms = "x64","x86","arm64","AnyCPU"
$env:Build_RootDirectory = (Split-Path $MyInvocation.MyCommand.Path)
$env:Build_Configuration = $Configuration
$env:sdk_version = & (Join-Path $PSScriptRoot "..\build\Scripts\CreateBuildInfo.ps1") -Version $SDKVersion -IsSdkVersion $true -IsAzurePipelineBuild $IsAzurePipelineBuild

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
if ($IsAzurePipelineBuild) {
  $nugetPath = "nuget.exe";
} else {
  $nugetPath = (Join-Path $PSScriptRoot "..\build\NugetWrapper.cmd")
}

New-Item -ItemType Directory -Force -Path "$PSScriptRoot\_build"
& $nugetPath restore (Join-Path $PSScriptRoot DevHomeSDK.sln)

Try {
  foreach ($platform in $buildPlatforms) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      $msbuildArgs = @(
        ("$PSScriptRoot\DevHomeSDK.sln"),
        ("/p:Platform="+$platform),
        ("/p:Configuration="+$configuration),
        ("/binaryLogger:DevHome.SDK.$platform.$configuration.binlog")
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

& $nugetPath pack (Join-Path $PSScriptRoot "nuget\Microsoft.Windows.DevHome.SDK.nuspec") -Version $env:sdk_version -OutputDirectory "$PSScriptRoot\_build"

if ($IsAzurePipelineBuild) {
  Write-Host "##vso[task.setvariable variable=SDKVersion;]$env:sdk_version"
  Write-Host "##vso[task.setvariable variable=SDKVersion;isOutput=true;]$env:sdk_version"
}

$TotalTime = (Get-Date)-$StartTime
$TotalMinutes = [math]::Floor($TotalTime.TotalMinutes)
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds)

Write-Host @"
Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN