Param(
    [string]$Platform = "x64",
    [string]$Configuration = "debug",
    [string]$VersionOfSDK,
    [string]$SDKNugetSource,
    [string]$Version,
    [string]$BuildStep = "all",
    [string]$AzureBuildingBranch = "main",
    [switch]$IsAzurePipelineBuild = $false,
    [switch]$Help = $false
)

$StartTime = Get-Date

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation.
Licensed under the MIT License.

Syntax:
      Build.cmd [options]

Description:
      Builds Dev Home.

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

$env:Build_RootDirectory = (Split-Path $MyInvocation.MyCommand.Path)
$env:Build_Platform = $Platform.ToLower()
$env:Build_Configuration = $Configuration.ToLower()
$env:msix_version = build\Scripts\CreateBuildInfo.ps1 -Version $Version -IsAzurePipelineBuild $IsAzurePipelineBuild
$env:sdk_version = build\Scripts\CreateBuildInfo.ps1 -Version $VersionOfSDK -IsSdkVersion $true -IsAzurePipelineBuild $IsAzurePipelineBuild

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')

if ($IsAzurePipelineBuild) {
  Copy-Item (Join-Path $env:Build_RootDirectory "build\nuget.config.internal") -Destination (Join-Path $env:Build_RootDirectory "nuget.config")
}

$ErrorActionPreference = "Stop"

if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "sdk")) {
  foreach ($configuration in $env:Build_Configuration.Split(",")) {
    extensionsdk\BuildSDKHelper.ps1 -Configuration $configuration -VersionOfSDK $env:sdk_version -IsAzurePipelineBuild $IsAzurePipelineBuild -BypassWarning
  }
}

if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "DevSetupAgent") -Or ($BuildStep -ieq "fullMsix")) {
  foreach ($configuration in $env:Build_Configuration.Split(",")) {
    # We use x86 DevSetupAgent for x64 and x86 Dev Home build. Only need to build it once if we are building multiple platforms.
    $builtX86 = $false
    foreach ($platform in $env:Build_Platform.Split(",")) {
      if ($platform -ieq "arm64") {
        HyperVExtension\BuildDevSetupAgentHelper.ps1 -Platform $Platform -Configuration $configuration -VersionOfSDK $env:sdk_version -SDKNugetSource $SDKNugetSource -AzureBuildingBranch $AzureBuildingBranch -IsAzurePipelineBuild $IsAzurePipelineBuild -BypassWarning
      }
      elseif (-not $builtX86) {
        HyperVExtension\BuildDevSetupAgentHelper.ps1 -Platform "x86" -Configuration $configuration -VersionOfSDK $env:sdk_version -SDKNugetSource $SDKNugetSource -AzureBuildingBranch $AzureBuildingBranch -IsAzurePipelineBuild $IsAzurePipelineBuild -BypassWarning
        $builtX86 = $true
      }
    }
  }
}

$msbuildPath = &"${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
if ($IsAzurePipelineBuild) {
  $nugetPath = "nuget.exe";
} else {
  $nugetPath = (Join-Path $env:Build_RootDirectory "build\NugetWrapper.cmd")
}

# Install NuGet Cred Provider
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-Expression "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) } -AddNetfx"

if (-not([string]::IsNullOrWhiteSpace($SDKNugetSource))) {
  & $nugetPath sources add -Source $SDKNugetSource
}

. build\Scripts\CertSignAndInstall.ps1

Try {
  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msix") -Or ($BuildStep -ieq "fullMsix")) {
    $buildRing = "Dev"
    $newPackageName = $null
    $newPackageDisplayName = $null
    $newAppDisplayNameResource = $null
    $newWidgetProviderDisplayName = $null

    if ($AzureBuildingBranch -ieq "release") {
      $buildRing = "Stable"
      $newPackageName = "Microsoft.Windows.DevHome"
      $newPackageDisplayName = "Dev Home (Preview)"
      $newAppDisplayNameResource = "ms-resource:AppDisplayNameStable"
      $newWidgetProviderDisplayName = "ms-resource:WidgetProviderDisplayNameStable"
    } elseif ($AzureBuildingBranch -ieq "staging") {
      $buildRing = "Canary"
      $newPackageName = "Microsoft.Windows.DevHome.Canary"
      $newPackageDisplayName = "Dev Home (Canary)"
      $newAppDisplayNameResource = "ms-resource:AppDisplayNameCanary"
      $newWidgetProviderDisplayName = "ms-resource:WidgetProviderDisplayNameCanary"
    }

    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    $xIdentity = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity");
    $xProperties = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Properties");
    $xDisplayName = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}DisplayName");
    $xApplications = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Applications");
    $xApplication = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Application");
    $uapVisualElements = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10}VisualElements");
    $xExtensions = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Extensions");
    $uapExtension = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10/3}Extension");
    $uapAppExtension = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/uap/windows10/3}AppExtension");

    # Update the appxmanifest
    $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\Package.appxmanifest")
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = $env:msix_version
    if (-not ([string]::IsNullOrEmpty($newPackageName))) {
      $appxmanifest.Root.Element($xIdentity).Attribute("Name").Value = $newPackageName
    }
    if (-not ([string]::IsNullOrEmpty($newPackageDisplayName))) {
      $appxmanifest.Root.Element($xProperties).Element($xDisplayName).Value = $newPackageDisplayName
    }
    if (-not ([string]::IsNullOrEmpty($newAppDisplayNameResource))) {
      $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($uapVisualElements).Attribute("DisplayName").Value = $newAppDisplayNameResource
      $extensions = $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($xExtensions).Elements($uapExtension)
      foreach ($extension in $extensions) {
        if ($extension.Attribute("Category").Value -eq "windows.appExtension") {
          $appExtension = $extension.Element($uapAppExtension)
          switch ($appExtension.Attribute("Name").Value) {
            "com.microsoft.devhome" {
              $appExtension.Attribute("DisplayName").Value = $newAppDisplayNameResource
            }
            "com.microsoft.windows.widgets" {
              $appExtension.Attribute("DisplayName").Value = $newWidgetProviderDisplayName
            }
          }
        }
      }
    }
    $appxmanifest.Save($appxmanifestPath)

    # This is needed for vcxproj
    & $nugetPath restore

    foreach ($platform in $env:Build_Platform.Split(",")) {
      foreach ($configuration in $env:Build_Configuration.Split(",")) {
        $appxPackageDir = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration")
        Write-Host "Building DevHome for EnvPlatform: $env:Build_Platform Platform: $platform Configuration: $configuration BundlePlatforms: $appxBundlePlatform Dir: $appxPackageDir Ring: $buildRing"
        $msbuildArgs = @(
            ("DevHome.sln"),
            ("/p:Platform="+$platform),
            ("/p:Configuration="+$configuration),
            ("/restore"),
            ("/binaryLogger:DevHome.$platform.$configuration.binlog"),
            ("/p:AppxBundle=Never"),
            ("/p:AppxPackageName=DevHome-$platform"),
            ("/p:AppxBundlePlatforms="+$platform),
            ("/p:AppxPackageDir=" + $appxPackageDir),
            ("/p:AppxPackageTestDir=" + (Join-Path $appxPackageDir "DevHome\")),
            ("/p:AppxPackageSigningEnabled=false"),
            ("/p:GenerateAppxPackageOnBuild=true"),
            ("/p:BuildRing=$buildRing")
        )
        if (-not([string]::IsNullOrWhiteSpace($VersionOfSDK))) {
          $msbuildArgs += ("/p:DevHomeSDKVersion="+$env:sdk_version)
        }
        if ($BuildStep -ieq "msix") {
          $msbuildArgs += ("/p:IgnoreZipPackages=true")
        }

        & $msbuildPath $msbuildArgs

        if (-not($IsAzurePipelineBuild) -And $isAdmin) {
          Invoke-SignPackage "$appxPackageDir\DevHome-$platform.msix"
        }
      }
    }

    # Reset the appxmanifest to prevent unnecessary code changes
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = "0.0.0.0"
    $appxmanifest.Root.Element($xIdentity).Attribute("Name").Value = "Microsoft.Windows.DevHome.Dev"
    $appxmanifest.Root.Element($xProperties).Element($xDisplayName).Value = "Dev Home (Dev)"
    $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($uapVisualElements).Attribute("DisplayName").Value = "ms-resource:AppDisplayNameDev"
    $extensions = $appxmanifest.Root.Element($xApplications).Element($xApplication).Element($xExtensions).Elements($uapExtension)
    foreach ($extension in $extensions) {
      if ($extension.Attribute("Category").Value -eq "windows.appExtension") {
        $appExtension = $extension.Element($uapAppExtension)
        switch ($appExtension.Attribute("Name").Value) {
          "com.microsoft.devhome" {
            $appExtension.Attribute("DisplayName").Value = "ms-resource:AppDisplayNameDev"
          }
          "com.microsoft.windows.widgets" {
            $appExtension.Attribute("DisplayName").Value = "ms-resource:WidgetProviderDisplayNameDev"
          }
        }
      }
    }
    $appxmanifest.Save($appxmanifestPath)
  }

  if (($BuildStep -ieq "stubpackages")) {
    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    $msbuildArgs = @(
      ("DevHomeStub\DevHomeStub.sln"),
      ("/p:Configuration=Release"),
      ("/restore"),
      ("/p:AppxPackageSigningEnabled=false")
      )

    # Update the appxmanifest
    $xIdentity = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity");
    $appxmanifestPath = (Join-Path $env:Build_RootDirectory "DevHomeStub\DevHomeStubPackage\Package.appxmanifest")
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $versionParts = ($env:msix_version).Split('.')
    $versionParts[1] = [string]([int]($versionParts[1]) - 1)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = ($versionParts -join '.')
    $appxmanifest.Save($appxmanifestPath)

    & $msbuildPath  $msbuildArgs
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = "0.0.0.0"
    $appxmanifest.Save($appxmanifestPath)
  }

  if (($BuildStep -ieq "all") -Or ($BuildStep -ieq "msixbundle")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      .\build\scripts\Create-AppxBundle.ps1 -InputPath (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration\DevHome") -ProjectName DevHome -BundleVersion ([version]$env:msix_version) -OutputPath (Join-Path $env:Build_RootDirectory ("AppxBundles\$configuration\DevHome_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle"))
      if (-not($IsAzurePipelineBuild) -And $isAdmin) {
        Invoke-SignPackage ("AppxBundles\$configuration\DevHome_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle")
      }
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
$TotalSeconds = [math]::Ceiling($TotalTime.TotalSeconds) - ($totalMinutes * 60)

if (-not($isAdmin)) {
  Write-Host @"

WARNING: Cert signing requires admin privileges.  To sign, run the following in an elevated Developer Command Prompt.
"@ -ForegroundColor GREEN
  foreach ($platform in $env:Build_Platform.Split(",")) {
    foreach ($configuration in $env:Build_Configuration.Split(",")) {
      $appxPackageFile = (Join-Path $env:Build_RootDirectory ("AppxPackages\$configuration\DevHome\DevHome-$platform.msix"))
        Write-Host @"
powershell -command "& { . build\scripts\CertSignAndInstall.ps1; Invoke-SignPackage $appxPackageFile }"
"@ -ForegroundColor GREEN
    }
  }
}

Write-Host @"

Total Running Time:
$TotalMinutes minutes and $TotalSeconds seconds
"@ -ForegroundColor CYAN