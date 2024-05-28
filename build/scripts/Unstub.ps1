# This script unstubs the telemetry at build time and replaces the stubbed file with a reference internal nuget package

#
# Unstub managed telemetry
#

Remove-Item "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry\TelemetryEventSource.cs"

$projFile = "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry\DevHome.Telemetry.csproj"
$projFileContent = Get-Content $projFile -Encoding UTF8 -Raw

$xml = [xml]$projFileContent
$xml.PreserveWhitespace = $true

$defineConstantsNode = $xml.SelectSingleNode("//DefineConstants")
if ($defineConstantsNode -ne $null) {
    $defineConstantsNode.ParentNode.RemoveChild($defineConstantsNode)
    $xml.Save($projFile)
}

if ($projFileContent.Contains('Microsoft.Telemetry.Inbox.Managed')) {
    Write-Output "Project file already contains a reference to the internal package."
    return;
}

$packageReferenceNode = $xml.CreateElement("PackageReference");
$packageReferenceNode.SetAttribute("Include", "Microsoft.Telemetry.Inbox.Managed")
$packageReferenceNode.SetAttribute("Version", "10.0.25148.1001-220626-1600.rs-fun-deploy-dev5")
$itemGroupNode = $xml.CreateElement("ItemGroup")
$itemGroupNode.AppendChild($packageReferenceNode)
$xml.DocumentElement.AppendChild($itemGroupNode)
$xml.Save($projFile)


#
# Unstub native telemetry
#

# Delete the existing stub .h
Remove-Item "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry.Native\inc\MicrosoftTelemetry.h"

# Load packages.config
$packagesConfig = "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry.Native\packages.config"
$xml = [xml](Get-Content $packagesConfig -Encoding UTF8 -Raw)
$xml.PreserveWhitespace = $true

# Create new <package>
#   e.g. <package id="Microsoft.Telemetry.Inbox.Native" version="10.0.18362.1-190318-1202.19h1-release.amd64fre" targetFramework="native" />
$packageNode = $xml.CreateElement("package");
$packageNode.SetAttribute("id", "Microsoft.Telemetry.Inbox.Native")
$packageNode.SetAttribute("version", "10.0.18362.1-190318-1202.19h1-release.amd64fre")
$packageNode.SetAttribute("targetFramework", "native")

# Append to <packages>
$packagesNode = $xml.SelectSingleNode("/packages")
$packagesNode.AppendChild($packageNode)

# Save
$xml.Save($packagesConfig)
