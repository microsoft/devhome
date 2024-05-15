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

function Write-XmlDocumentToFile {
  param (
    [System.Xml.XmlDocument]$xmlDocument,
    [string]$filePath
  )

  $settings = New-Object System.Xml.XmlWriterSettings
  $settings.Indent = $true
  $settings.CheckCharacters = $false
  $settings.NewLineChars = "`r`n"

  $writer = [System.Xml.XmlWriter]::Create($filePath, $settings)
  $xmlDocument.WriteTo($writer)
  $writer.Flush()
  $writer.Close()
}

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
        extensions\HyperVExtension\BuildDevSetupAgentHelper.ps1 -Platform $Platform -Configuration $configuration -VersionOfSDK $env:sdk_version -SDKNugetSource $SDKNugetSource -AzureBuildingBranch $AzureBuildingBranch -IsAzurePipelineBuild $IsAzurePipelineBuild -BypassWarning
      }
      elseif (-not $builtX86) {
        extensions\HyperVExtension\BuildDevSetupAgentHelper.ps1 -Platform "x86" -Configuration $configuration -VersionOfSDK $env:sdk_version -SDKNugetSource $SDKNugetSource -AzureBuildingBranch $AzureBuildingBranch -IsAzurePipelineBuild $IsAzurePipelineBuild -BypassWarning
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
    # Update C++ version resources and header
    $cppHeader = (Join-Path $env:Build_RootDirectory "build\cppversion\version.h")
    $updatebinverpath = (Join-Path $env:Build_RootDirectory "build\scripts\update-binver.ps1")
    & $updatebinverpath -TargetFile $cppHeader -BuildVersion $env:msix_version

    $buildRing = "Dev"
    $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\Package-Dev.appxmanifest")

    if ($AzureBuildingBranch -ieq "release") {
      $buildRing = "Stable"
      $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\Package.appxmanifest")
    } elseif ($AzureBuildingBranch -ieq "staging") {
      $buildRing = "Canary"
      $appxmanifestPath = (Join-Path $env:Build_RootDirectory "src\Package-Can.appxmanifest")
    }

    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq")
    $xIdentity = [System.Xml.Linq.XName]::Get("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity");

    # Update the appxmanifest
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = $env:msix_version

    Write-XmlDocumentToFile -xmlDocument $appxmanifest -filePath $appxmanifestPath

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
            ("/p:Version="+$env:msix_version),
            ("/restore"),
            ("/binaryLogger:DevHome.$platform.$configuration.binlog"),
            ("/p:AppxPackageOutput=$appxPackageDir\DevHome-$platform.msix"),
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

    # reset version file back to original values
    $cppHeader = (Join-Path $env:Build_RootDirectory "build\cppversion\version.h")
    $updatebinverpath = (Join-Path $env:Build_RootDirectory "build\scripts\update-binver.ps1")
    & $updatebinverpath -TargetFile $cppHeader -BuildVersion "1.0.0.0"

    # Reset the appxmanifest to prevent unnecessary code changes
    $appxmanifest = [System.Xml.Linq.XDocument]::Load($appxmanifestPath)
    $appxmanifest.Root.Element($xIdentity).Attribute("Version").Value = "0.0.0.0"
    Write-XmlDocumentToFile -xmlDocument $appxmanifest -filePath $appxmanifestPath
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
      .\build\scripts\Create-AppxBundle.ps1 -InputPath (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration") -ProjectName DevHome -BundleVersion ([version]$env:msix_version) -OutputPath (Join-Path $env:Build_RootDirectory ("AppxBundles\$configuration\DevHome_" + $env:msix_version + "_8wekyb3d8bbwe.msixbundle"))
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
      $appxPackageFile = (Join-Path $env:Build_RootDirectory "AppxPackages\$configuration\DevHome-$platform.msix")
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