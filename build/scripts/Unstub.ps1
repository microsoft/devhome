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
