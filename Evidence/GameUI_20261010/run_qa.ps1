param([string]$Out = "Evidence/GameUI_20261010/StandaloneQA")
$ErrorActionPreference = "Stop"
$taskRoot = (Get-Location).Path
$taskExe = Join-Path $taskRoot "Builds/Windows_Economy_UI_20261010/Cay_The_Gioi.exe"
$taskOut = [IO.Path]::GetFullPath((Join-Path $taskRoot $Out))
if (-not $taskOut.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Evidence must stay inside workspace" }
if (Test-Path -LiteralPath $taskOut) { throw "Use a fresh directory to preserve previous evidence" }
$taskSave = Join-Path $taskOut "save"
$taskReports = Join-Path $taskOut "reports"
$taskShots = Join-Path $taskOut "screenshots"
$taskLogs = Join-Path $taskOut "logs"
New-Item -ItemType Directory -Path $taskSave,$taskReports,$taskShots,$taskLogs | Out-Null
$taskReal = Join-Path $env:LOCALAPPDATA "../LocalLow/DefaultCompany/Cay_The_Gioi"
function Get-PlayerHashes {
    $h = @{}
    foreach ($n in @("world_tree_save.json","world_tree_save.backup.json")) {
        $p = Join-Path $taskReal $n
        $h[$n] = if (Test-Path -LiteralPath $p) { (Get-FileHash -LiteralPath $p).Hash } else { "MISSING" }
    }
    return $h
}
$taskBefore = Get-PlayerHashes
$taskBefore | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOut "hashes_before.json")
function Invoke-GameScenario([string]$Scenario,[long]$Utc) {
    $report = Join-Path $taskReports ($Scenario + ".json")
    $log = Join-Path $taskLogs ($Scenario + ".log")
    $taskScenarioArgs = "-screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -ctg-save-dir `"$taskSave`" -ctg-scenario $Scenario -ctg-report `"$report`" -ctg-shots `"$taskShots`" -logFile `"$log`""
    if ($Utc -gt 0) { $taskScenarioArgs += " -ctg-utc $Utc" }
    $proc = Start-Process -FilePath $taskExe -ArgumentList $taskScenarioArgs -WindowStyle Hidden -PassThru
    if (-not $proc.WaitForExit(90000)) { $proc.Kill(); throw "Scenario timeout: $Scenario" }
    Write-Host "$Scenario exit=$($proc.ExitCode)"
    return $proc.ExitCode
}
$taskResults = [ordered]@{}
$taskResults.smoke_fresh = Invoke-GameScenario "smoke_fresh" 0
$taskUtc = [long](Get-Content -LiteralPath (Join-Path $taskSave "world_tree_save.json") -Raw | ConvertFrom-Json).savedAtUtcSeconds
$taskResults.smoke_offline = Invoke-GameScenario "smoke_offline" ($taskUtc + 3600)
$taskResults.smoke_progression = Invoke-GameScenario "smoke_progression" 0
$taskResults.smoke_death_forced_rebirth = Invoke-GameScenario "smoke_death_forced_rebirth" 0
$taskResults.ui_flow = Invoke-GameScenario "ui_flow" 0
$taskAfter = Get-PlayerHashes
$taskAfter | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOut "hashes_after.json")
$taskSame = @($taskBefore.Keys | Where-Object { $taskBefore[$_] -ne $taskAfter[$_] }).Count -eq 0
$taskSummary = [ordered]@{exitCodes=$taskResults;realSaveUnchanged=$taskSame;exe=$taskExe;exeSha256=(Get-FileHash -LiteralPath $taskExe).Hash;runAt=(Get-Date).ToString("s")}
$taskSummary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskOut "summary.json")
$taskSummary | ConvertTo-Json -Depth 5
