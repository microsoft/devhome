param (
    [string]$Platform = "x64"
)

Write-Host "Installing WinAppRuntime"
Invoke-WebRequest -Uri "https://aka.ms/windowsappsdk/1.5/latest/windowsappruntimeinstall-x64.exe" -OutFile "$PSScriptRoot\winappsdk.exe"
& "$PSScriptRoot\winappsdk.exe"

Write-Host "Checking if we are in Windows 11"
$isWin11 = (Get-WmiObject Win32_OperatingSystem).Caption -Match "Windows 11"
if ($isWin11){
    Write-Host "We are in Windows 11. Installing Widgets Runtime."
    & winget install --id 9N3RK8ZV2ZR8 --accept-package-agreements --accept-source-agreements --force
}

Write-Host "Starting WinAppDriver"
Start-Process -FilePath "C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe"

Write-Host "Installing Dev Home"
Write-Host "$PSScriptRoot\..\msix\DevHome-$Platform.msix"
Add-AppPackage "$PSScriptRoot\..\msix\DevHome-$Platform.msix"

if ($true) {
    # Start/stop the app once so that WinAppDriver doesn't time out during first time setup
    # and wait 60 seconds to give plenty of time
    Write-Host "Starting Dev Home"
    Start-Process "Shell:AppsFolder\Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App"
    Start-Sleep 60
    Stop-Process -Name "DevHome"
}