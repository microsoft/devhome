<#
.SYNOPSIS
 
Appends the existing Windows PowerShell PSModulePath to existing PSModulePath.
This PowerShell module has been taken from https://www.powershellgallery.com/packages/WindowsPSModulePath/1.0.0
which Microsoft owns the Copywrite to. It is functionally the same but We do not need to download it.
 
.DESCRIPTION
 
If the current PSModulePath does not contain the Windows PowerShell PSModulePath, it will
be appended to the end.
#>

function Add-WindowsPSModulePath
{

    if (! $IsWindows)
    {
        throw "This cmdlet is only supported on Windows"
    }

    $WindowsPSModulePath = [System.Environment]::GetEnvironmentVariable("psmodulepath", [System.EnvironmentVariableTarget]::Machine)
    if (-not ($env:PSModulePath).Contains($WindowsPSModulePath))
    {
        $env:PSModulePath += ";${env:userprofile}\Documents\WindowsPowerShell\Modules;${env:programfiles}\WindowsPowerShell\Modules;${WindowsPSModulePath}"
    }
}