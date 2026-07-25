# Timberborn Autopilot — Brain Training watchdog.
# Runs game episodes in a loop: launch -> autopilot plays -> episode ends (mod
# exits the game) -> score -> mutate parameters (hill climb) -> relaunch.
# Stop anytime: create the file  Documents\Timberborn\Autopilot\STOP  or Ctrl+C.
param(
    [string]$GameExe = "D:\SteamLibrary\steamapps\common\Timberborn\Timberborn.exe",
    [int]$MaxEpisodes = 0,               # 0 = run until stopped
    [int]$EpisodeTimeoutMinutes = 75,
    [string]$FactionId = "Folktails",
    [string]$MapName = "Plains",
    [string]$SettlementName = "TrainingRun",
    [int]$MaxCycles = 6,
    [double]$GameSpeed = 10
)

$ErrorActionPreference = "Stop"
$docs = [Environment]::GetFolderPath('MyDocuments')
$auto = Join-Path $docs "Timberborn\Autopilot"
$savesRoot = Join-Path $docs "Timberborn\Saves"
New-Item -ItemType Directory -Force $auto | Out-Null
$paramsPath = Join-Path $auto "params.json"
$bestPath = Join-Path $auto "best-params.json"
$historyPath = Join-Path $auto "training-history.jsonl"
$resultPath = Join-Path $auto "last-result.json"
$trainingPath = Join-Path $auto "training.json"
$stopPath = Join-Path $auto "STOP"

$defaults = [ordered]@{
    TreeMarkRadius = 12; CarrotZoneHalf = 5; PineZoneHalf = 6
    LumberjackTarget = 2; TankTarget = 2; LodgeTarget = 2
    SecondPumpWaterDays = 1.5; SecondPumpEarliestDay = 3
    PlanningTickInterval = 30; PumpSearchRadius = 30; DefaultSearchRadius = 15
}
$intKeys = @("TreeMarkRadius","CarrotZoneHalf","PineZoneHalf","LumberjackTarget",
             "TankTarget","LodgeTarget","SecondPumpEarliestDay","PlanningTickInterval",
             "PumpSearchRadius","DefaultSearchRadius")

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@
function Send-Enter {
    try {
        $p = Get-Process Timberborn -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
        if ($p) {
            [Win32]::ShowWindow($p.MainWindowHandle, 9) | Out-Null   # SW_RESTORE
            [Win32]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
            Start-Sleep -Milliseconds 500
            [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
        }
    } catch { Write-Host "  Send-Enter failed: $($_.Exception.Message)" -ForegroundColor Yellow }
}
Add-Type -AssemblyName System.Windows.Forms

function Read-Json($path, $fallback) {
    if (Test-Path $path) { try { return Get-Content $path -Raw | ConvertFrom-Json } catch {} }
    return $fallback
}
function To-Hashtable($obj) {
    $h = [ordered]@{}
    foreach ($p in $obj.PSObject.Properties) { $h[$p.Name] = $p.Value }
    return $h
}
function Mutate($base) {
    $p = To-Hashtable $base
    $keys = @($p.Keys)
    $count = Get-Random -Minimum 1 -Maximum 3   # mutate 1-2 params
    for ($i = 0; $i -lt $count; $i++) {
        $k = $keys | Get-Random
        $factor = (Get-Random -Minimum 75 -Maximum 126) / 100.0
        $v = [double]$p[$k] * $factor
        if ($intKeys -contains $k) { $v = [Math]::Max(1, [Math]::Round($v)) }
        else { $v = [Math]::Round([Math]::Max(0.5, [Math]::Min(4.0, $v)), 2) }
        $p[$k] = $v
    }
    return $p
}

# Baseline: existing best, else existing params, else defaults.
$best = Read-Json $bestPath (Read-Json $paramsPath ([pscustomobject]$defaults))
$bestScore = -999999
$history = @()
if (Test-Path $historyPath) {
    Get-Content $historyPath | ForEach-Object {
        try { $h = $_ | ConvertFrom-Json; if ($h.score -gt $bestScore) { $bestScore = $h.score } } catch {}
    }
}
Write-Host "=== Brain Training watchdog === best score so far: $bestScore" -ForegroundColor Green
Write-Host "Stop with Ctrl+C or by creating: $stopPath"

$episode = 0
try {
    while ($true) {
        if (Test-Path $stopPath) { Write-Host "STOP file found - ending."; break }
        if ($MaxEpisodes -gt 0 -and $episode -ge $MaxEpisodes) { break }
        $episode++

        # First episode runs the current best/baseline unmutated; then mutate.
        if ($episode -eq 1) { $current = To-Hashtable $best } else { $current = Mutate $best }
        ($current | ConvertTo-Json) | Out-File -Encoding utf8 $paramsPath
        (@{ Enabled = $true; FactionId = $FactionId; MapName = $MapName
            SettlementName = $SettlementName; MaxCycles = $MaxCycles
            GameSpeed = $GameSpeed } | ConvertTo-Json) | Out-File -Encoding utf8 $trainingPath
        if (Test-Path $resultPath) { Remove-Item $resultPath -Force }

        Write-Host ("[{0}] Episode {1}: launching game..." -f (Get-Date -Format 'HH:mm:ss'), $episode)
        # Launch with NO args (custom args trigger a Steam confirmation prompt
        # that blocks unattended runs). The mod-manager OK screen is dismissed
        # by sending Enter to the window instead (the panel advances on Enter).
        Start-Process -FilePath $GameExe | Out-Null
        # Steam bootstrap: the launched exe may exit and respawn via Steam,
        # so track the game by PROCESS NAME, not the launcher handle.
        $appeared = $false
        $appearDeadline = (Get-Date).AddSeconds(180)
        while ((Get-Date) -lt $appearDeadline) {
            if (Get-Process Timberborn -ErrorAction SilentlyContinue) { $appeared = $true; break }
            Start-Sleep -Seconds 5
        }
        if (-not $appeared) {
            Write-Host "  Game process never appeared - recording crash." -ForegroundColor Yellow
        }
        else {
            # Give the mod screen time to render, then press Enter a few times
            # to advance past it into the main menu.
            Start-Sleep -Seconds 25
            Send-Enter
            Start-Sleep -Seconds 3
            Send-Enter
            $deadline = (Get-Date).AddMinutes($EpisodeTimeoutMinutes)
            while ((Get-Process Timberborn -ErrorAction SilentlyContinue) -and
                   (Get-Date) -lt $deadline -and -not (Test-Path $stopPath)) {
                Start-Sleep -Seconds 15
            }
            if (Get-Process Timberborn -ErrorAction SilentlyContinue) {
                Write-Host "  Timeout/stop - killing game." -ForegroundColor Yellow
                try { Stop-Process -Name Timberborn -Force -Confirm:$false } catch {}
                Start-Sleep -Seconds 10
            }
        }

        $result = Read-Json $resultPath ([pscustomobject]@{ result = "crash"; score = -2000; reason = "no result written" })
        $entry = @{ episode = $episode; time = (Get-Date -Format 'o'); params = $current
                    result = $result.result; score = $result.score; reason = $result.reason }
        ($entry | ConvertTo-Json -Compress -Depth 5) | Add-Content -Encoding utf8 $historyPath
        Write-Host ("  Result: {0} score {1} - {2}" -f $result.result, $result.score, $result.reason)

        # Keep winners.
        if ($result.score -gt $bestScore) {
            $bestScore = $result.score
            $best = [pscustomobject]$current
            ($best | ConvertTo-Json) | Out-File -Encoding utf8 $bestPath
            Write-Host "  NEW BEST ($bestScore) - params saved." -ForegroundColor Green
        }

        # Clean the training save so the next episode starts fresh.
        Get-ChildItem $savesRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like "$SettlementName*" } |
            ForEach-Object { Remove-Item $_.FullName -Recurse -Force -Confirm:$false }
        Start-Sleep -Seconds 5
    }
}
finally {
    # Never leave training mode armed for normal play.
    (@{ Enabled = $false } | ConvertTo-Json) | Out-File -Encoding utf8 $trainingPath
    if (Test-Path $stopPath) { Remove-Item $stopPath -Force }
    Write-Host "Training stopped. training.json disabled. Best score: $bestScore" -ForegroundColor Green
}
