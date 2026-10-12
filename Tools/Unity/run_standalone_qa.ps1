# T09 Windows standalone QA runner.
# Chay ban build voi save thu RIENG (-ctg-save-dir), khong dung save that cua nguoi choi.
# Moi kich ban ghi report JSON + screenshot + Player.log copy vao Evidence/T09/StandaloneQA/.
# Kich ban la QA hook (goi API he thong), khong phai thao tac chuot that; xem Docs/Validation/T09_*.md.
param(
    [string]$Exe = "Builds\Windows\CayTheGioi.exe",
    [string]$Out = "Evidence\T09\StandaloneQA",
    [int]$OfflineSeconds = 3600
)
$ErrorActionPreference = "Stop"
$root = (Get-Location).Path
$exePath = Join-Path $root $Exe
$outDir = Join-Path $root $Out
$saveDir = Join-Path $outDir "qa_save"
$repDir = Join-Path $outDir "reports"
$shotDir = Join-Path $outDir "screenshots"
$logDir = Join-Path $outDir "logs"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Force $saveDir, $repDir, $shotDir, $logDir | Out-Null

$playerLog = Join-Path $env:USERPROFILE "AppData\LocalLow\DefaultCompany\Cay_The_Gioi\Player.log"
$realDir = Join-Path $env:USERPROFILE "AppData\LocalLow\DefaultCompany\Cay_The_Gioi"
$hashFiles = "world_tree_save.json", "world_tree_save.backup.json"
function Get-RealHashes { $h = @{}; foreach ($f in $hashFiles) { $p = Join-Path $realDir $f; $h[$f] = if (Test-Path $p) { (Get-FileHash $p).Hash } else { "MISSING" } }; $h }
$before = Get-RealHashes
$before | ConvertTo-Json | Set-Content (Join-Path $outDir "player_save_hashes_before.json")

function Invoke-Scenario([string]$name, [long]$utc) {
    $rep = Join-Path $repDir "$name.json"
    # Duong dan co khoang trang (vd "foder game 3D") phai duoc boc nhay trong chuoi tham so.
    $argLine = "-screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -ctg-save-dir `"$saveDir`" -ctg-scenario $name -ctg-report `"$rep`" -ctg-shots `"$shotDir`""
    if ($utc -gt 0) { $argLine += " -ctg-utc $utc" }
    $p = Start-Process -FilePath $exePath -ArgumentList $argLine -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(90000)) { $p.Kill(); Write-Host "TIMEOUT $name"; return -999 }
    Copy-Item $playerLog (Join-Path $logDir "$name.Player.log") -Force -ErrorAction SilentlyContinue
    return $p.ExitCode
}
function Get-SaveUtc { $j = Get-Content (Join-Path $saveDir "world_tree_save.json") -Raw | ConvertFrom-Json; [long]$j.savedAtUtcSeconds }

$results = [ordered]@{}
$results["smoke_fresh"] = Invoke-Scenario "smoke_fresh" 0
$utc = Get-SaveUtc
Copy-Item (Join-Path $saveDir "world_tree_save.json") (Join-Path $outDir "save_after_smoke_fresh.json")
$results["smoke_offline"] = Invoke-Scenario "smoke_offline" ($utc + $OfflineSeconds)
Copy-Item (Join-Path $saveDir "world_tree_save.json") (Join-Path $outDir "save_after_smoke_offline.json")
$results["smoke_progression"] = Invoke-Scenario "smoke_progression" (Get-SaveUtc)
Copy-Item (Join-Path $saveDir "world_tree_save.json") (Join-Path $outDir "save_after_smoke_progression.json")
$results["smoke_death_forced_rebirth"] = Invoke-Scenario "smoke_death_forced_rebirth" (Get-SaveUtc)
Copy-Item (Join-Path $saveDir "world_tree_save.json") (Join-Path $outDir "save_after_smoke_death_forced_rebirth.json")
$results["ui_flow"] = Invoke-Scenario "ui_flow" (Get-SaveUtc)
Copy-Item (Join-Path $saveDir "world_tree_save.json") (Join-Path $outDir "save_after_ui_flow.json")

$after = Get-RealHashes
$after | ConvertTo-Json | Set-Content (Join-Path $outDir "player_save_hashes_after.json")
$same = (($before.Keys | Where-Object { $before[$_] -ne $after[$_] }).Count -eq 0)
$summary = [ordered]@{ exitCodes = $results; realSaveUnchanged = $same; exe = $Exe; exeLastWrite = (Get-Item $exePath).LastWriteTime.ToString("s"); runAt = (Get-Date).ToString("s") }
$summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $outDir "summary.json")
$summary | ConvertTo-Json -Depth 4
