$ErrorActionPreference = 'Stop'
$reviewRoot = $PSScriptRoot
$projectRoot = Split-Path (Split-Path $reviewRoot -Parent) -Parent
$exePath = Join-Path $projectRoot 'Builds\Windows_T10_20261007\Cay_The_Gioi.exe'
$runRoot = Join-Path $reviewRoot ('standalone_' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $runRoot | Out-Null
$results = @()
foreach ($scenario in @('smoke_fresh','smoke_offline','smoke_progression','smoke_death_forced_rebirth','ui_flow','smoke_raycast')) {
    $caseRoot = Join-Path $runRoot $scenario
    $saveRoot = Join-Path $caseRoot 'save'
    $shotsRoot = Join-Path $caseRoot 'shots'
    New-Item -ItemType Directory -Path $saveRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $shotsRoot -Force | Out-Null
    $reportPath = Join-Path $caseRoot 'report.json'
    $logPath = Join-Path $caseRoot 'player.log'
    $utcBase = 2000000000
    if ($scenario -eq 'smoke_offline') {
        $fixture = Join-Path $projectRoot 'Evidence\T10\Standalone_Smoke\qa_save\world_tree_save.json'
        $fixtureDto = Get-Content -LiteralPath $fixture -Raw | ConvertFrom-Json
        $fixtureDto.run.levels.root = 1
        $fixtureDto.run.levels.leaves = 1
        $fixtureDto | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $saveRoot 'world_tree_save.json') -Encoding utf8NoBOM
        $utcBase = [long]$fixtureDto.offlineCheckpointUtcSeconds + 3600
    }
    $arguments = '-batchmode -ctg-save-dir "{0}" -ctg-scenario {1} -ctg-report "{2}" -ctg-shots "{3}" -ctg-utc {4} -logFile "{5}" -screen-width 1920 -screen-height 1080 -windowed' -f $saveRoot,$scenario,$reportPath,$shotsRoot,$utcBase,$logPath
    $process = Start-Process -FilePath $exePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { throw "Scenario timed out: $scenario (PID $($process.Id))" }
    $process.Refresh()
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    $errors = @(Select-String -LiteralPath $logPath -Pattern 'NullReferenceException|MissingReferenceException|Unhandled Exception|Shader error|CHECK_FAIL')
    $results += [PSCustomObject]@{ scenario=$scenario; exitCode=$process.ExitCode; success=$report.success; error=$report.error; runtimeErrors=$errors.Count; reportPath=$reportPath; logPath=$logPath }
}
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runRoot 'summary.json') -Encoding utf8
$results | ConvertTo-Json -Depth 5
