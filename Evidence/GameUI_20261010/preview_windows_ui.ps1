# Interactive game preview: visible for reviewing the delivered UI; isolated QA save.
$ErrorActionPreference = "Stop"
$taskRoot = (Get-Location).Path
$taskOut = Join-Path $taskRoot "Evidence/GameUI_20261010/VisibleUiPreview"
if (Test-Path -LiteralPath $taskOut) { throw "Preserve earlier preview evidence" }
$taskSave = Join-Path $taskOut "save"
$taskShots = Join-Path $taskOut "screenshots"
New-Item -ItemType Directory -Path $taskOut,$taskSave,$taskShots | Out-Null
$taskExe = Join-Path $taskRoot "Builds/Windows_Economy_UI_20261010/Cay_The_Gioi.exe"
$taskReport = Join-Path $taskOut "ui_flow.json"
$taskLog = Join-Path $taskOut "Player.log"
$taskArguments = "-screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -ctg-save-dir `"$taskSave`" -ctg-scenario ui_flow -ctg-report `"$taskReport`" -ctg-shots `"$taskShots`" -logFile `"$taskLog`""
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle Normal -PassThru
if (-not $taskProcess.WaitForExit(90000)) { $taskProcess.Kill(); throw "UI preview timeout" }
Write-Host "Interactive UI flow exit=$($taskProcess.ExitCode)"
