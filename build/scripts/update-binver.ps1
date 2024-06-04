<#
.SYNOPSIS
    Updates the given version header file to match the version info passed in as a string.
.DESCRIPTION
    Updates the given header file with the version information passed in.  The version information is passed in as a string in the format "Major.Minor.Build.Revision".
    to match.  See the existing version.h for the format.
.PARAMETER TargetFile
    The file to update with version information.
.PARAMETER BuildVersion
    The build version to use.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$TargetFile,

    [Parameter(Mandatory=$true)]
    [string]$BuildVersion = "1.0.0.0"
)


$VersionParts = $BuildVersion.Split('.')
$MajorVersion = $VersionParts[0]
$MinorVersion = $VersionParts[1]
$BuildVersion = $VersionParts[2]

Write-Host "Using version: $MajorVersion.$MinorVersion.$BuildVersion.0"

if (![String]::IsNullOrEmpty($TargetFile))
{
    $Local:FullPath = Resolve-Path $TargetFile
    Write-Host "Updating file: $Local:FullPath"
    if (Test-Path $TargetFile)
    {
        $Local:ResultContent = ""
        foreach ($Local:line in [System.IO.File]::ReadLines($Local:FullPath))
        {
            if ($Local:line.StartsWith("#define VERSION_MAJOR"))
            {
                $Local:ResultContent += "#define VERSION_MAJOR $MajorVersion";
            }
            elseif ($Local:line.StartsWith("#define VERSION_MINOR"))
            {
                $Local:ResultContent += "#define VERSION_MINOR $MinorVersion";
            }
            elseif ($Local:line.StartsWith("#define VERSION_BUILD"))
            {
                $Local:ResultContent += "#define VERSION_BUILD $BuildVersion";
            }
            else
            {
                $Local:ResultContent += $Local:line;
            }
            $Local:ResultContent += [System.Environment]::NewLine;
        }
        Set-Content -Path $Local:FullPath -Value $Local:ResultContent
    }
    else
    {
        Write-Error "Did not find target file: $TargetFile"
    }
}
