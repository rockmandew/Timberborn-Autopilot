# Builds the Autopilot mod and deploys it to the Timberborn Mods folder.
param(
    [string]$GameDir = "D:\SteamLibrary\steamapps\common\Timberborn"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

dotnet build "$root\src\AutopilotMod\AutopilotMod.csproj" -c Release "-p:GameDir=$GameDir"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$docs = [Environment]::GetFolderPath('MyDocuments')
$modDir = Join-Path $docs "Timberborn\Mods\Autopilot"
New-Item -ItemType Directory -Force $modDir | Out-Null

Copy-Item "$root\mod\manifest.json" $modDir -Force
Copy-Item "$root\src\AutopilotMod\bin\Release\TimberbornAutopilot.dll" $modDir -Force

Write-Host "Deployed to $modDir" -ForegroundColor Green
