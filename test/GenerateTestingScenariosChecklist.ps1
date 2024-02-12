Param(
    [string]$FileName = "TestingScenariosChecklist.xlsx",
    [string]$GitHubExt = "..\..\devhomegithubextension",
    [string]$AzureExt = "..\..\devhomeazureextension",
    [switch]$Help = $false
)

if ($Help) {
    Write-Host @"
Copyright (c) Microsoft Corporation..
Licensed under the MIT License.

Syntax:
      GenerateTestingScenariosChecklist.ps1 [options]

Description:
      Generates a testing scenario validation Excel workbook

Options:

  -FileName <name>
      Name of the Excel workbook
      Example -FileName "TestingScenariosChecklist.xlsx"

  -GitHubExt <filepath>
      Path to the GitHub Extension repo on the same device
      Example: -GitHubExt "..\..\devhomegithubextension"

  -AzureExt <filepath>
      Path to the Azure Extension repo on the same device
      Example: -AzureExt "..\..\devhomeazureextension"

  -Help
      Display this usage message.
"@
  Exit
}

if (-not $fileName.EndsWith(".xlsx")) {
  $fileName += ".xlsx"
}

if (Test-Path $fileName) {
  Remove-Item $fileName
}

Install-Module -Name ImportExcel
Import-Module -Name ImportExcel

$repos = "..",$GitHubExt,$AzureExt
$worksheetNames = "Dev Home","GitHub Extension","Azure Extension"

For ($i=0;$i -lt $repos.count;$i++) {
  if (Test-Path $repos[$i]) {
    $files = Get-ChildItem $repos[$i] -recurse -Filter *.md | Where-Object {$_.FullName -like "*TestingScenarios*"}

    $testNumber = 0
    $data = "Test No;Test Scenario;Sign Off (Win10);Sign Off (Win11);Additional Sign Offs;Found Issues`n"

    ForEach ($file in $files) {
      $content = Get-Content $file.FullName
  
      ForEach ($line in $content) {
        if (-not $line.EndsWith(".md)") -and $line.StartsWith("1. ")) {
          $testNumber++
          $testScenario = $line.substring(3)
          $data += "" + $testNumber + ";" + $testScenario + "`n"
        }
      }
    }

    $data = ConvertFrom-Csv $data -Delimiter ";"

    $data | Export-Excel $fileName -WorksheetName $worksheetNames[$i]
  }
}