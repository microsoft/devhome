# This script unstubs the telemetry at build time and replaces the stubbed file with a reference internal nuget package

Remove-Item "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry\TelemetryEventSource.cs"

$projFile = "$($PSScriptRoot)\..\..\telemetry\DevHome.Telemetry\DevHome.Telemetry.csproj"
$projFileContent = Get-Content $projFile -Encoding UTF8 -Raw

if ($projFileContent.Contains('Microsoft.Telemetry.Inbox.Managed')) {
    Write-Output "Project file already contains a reference to the internal package."
    return;
}

$xml = [xml]$projFileContent
$xml.PreserveWhitespace = $true
$packageRef = $xml.SelectSingleNode("//ItemGroup/PackageReference")
$newNode = $packageRef.Clone()
$newNode.Include="Microsoft.Telemetry.Inbox.Managed"
$newNode.Version="10.0.25148.1001-220626-1600.rs-fun-deploy-dev5"
$parentNode = $packageRef.ParentNode
$parentNode.AppendChild($newNode)
$xml.Save($projFile)