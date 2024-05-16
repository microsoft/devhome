# Quick script to capture log files and create a zip file for upload to GitHub
# Currently this only creates one based on Date. We could add a time, so that 
# the ISV can have more than one Zip file

$MyZip=$(Get-Date -Format yyyy-MM-dd) + "DevHome.Zip"
$LogPath="C:\Users\" + $Env:UserName  + "\AppData\Local\Packages"
$LogFiles = Get-ChildItem -Path $LogPath -Filter "*.dhlog" -recurse
$Temp = [System.IO.Path]::GetTempPath()
$TempFile=$Temp+"\"+$MyZip

foreach ($File in $LogFiles) {
	Write-Host $File.FullName
	Compress-Archive -Path $File.FullName -DestinationPath $TempFile -Update
}
write-host Your log file is here: 
write-host $TempFile


