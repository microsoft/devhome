# Script to capture log files and create a zip file for upload to GitHub or log analysis.

param(
    [switch]$StopDevHome = $false
)

# List of supported package names for this tool. If a new Dev Home package is added
# with the same log format, add it to this list.
$devHomePackageNames = @(
    'Microsoft.Windows.DevHome_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeAzureExtension_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeAzureExtension.Canary_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeAzureExtension.Dev_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeGitHubExtension_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeGitHubExtension.Canary_8wekyb3d8bbwe'
    'Microsoft.Windows.DevHomeGitHubExtension.Dev_8wekyb3d8bbwe'
)

# Terminate Dev Home processes if user passed StopDevHome flag.
if ($StopDevHome)
{
    Write-Host "Stopping all Dev Home processes"
    Get-Process *DevHome* -ErrorAction Continue | Stop-Process
}

$LogFolderName = $(Get-Date -Format yyyy-MM-dd-HHmmss_) + "DevHomeLogs"
$TempRoot = [System.IO.Path]::GetTempPath()
$TempFolder = Join-Path -Path $TempRoot -ChildPath $LogFolderName
New-Item -ItemType Directory -Force -Path $TempFolder | Out-Null

# Only collect logs from known Dev Home packages
# Preserve folder structure to ensure no collisions in the archive.
$AppDataPackagesPath = Join-Path -Path $env:LOCALAPPDATA -ChildPath Packages
ForEach ($packageName in $devHomePackageNames)
{
    $packageFolderPath = Join-Path -Path $AppDataPackagesPath -ChildPath $packageName
    if (!(Test-Path $packageFolderPath))
    {
        Continue
    }
 
    $targetRoot = Join-Path -Path $TempFolder -ChildPath $packageName
    & robocopy /s $packageFolderPath $targetRoot *.dhlog | Out-Null
}

# Archive the folder and then remove the temp folder.
$ArchiveFileName = $LogFolderName + ".zip"
$ArchiveFilePath = Join-Path -Path $TempRoot -ChildPath $ArchiveFileName
$ArchiveSourcePath = Join-Path -Path $TempFolder -ChildPath "\*"
Compress-Archive -Path $ArchiveSourcePath -DestinationPath $ArchiveFilePath
Remove-Item $TempFolder -Recurse

Set-Clipboard -Value $ArchiveFilePath
Write-Host "Logs are archived in the following location, which was copied to your clipboard:"
Write-Host $ArchiveFilePath
