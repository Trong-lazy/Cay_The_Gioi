# Helper thu cong cho T09: mo ban build voi save RIENG, chup cua so bang PrintWindow, gui chuot that bang mouse_event.
# Toa do Click la pixel trong anh chup (goc tren-trai cua CUA SO, gom thanh tieu de).
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public class W32 {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, int d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
}
"@
[void][W32]::SetProcessDPIAware()
$script:Proc = $null
$script:ShotDir = $null

function Start-Player([string]$SaveDir, [string]$ShotDir, [long]$Utc = 0, [int]$W = 1600, [int]$H = 900) {
    $script:ShotDir = $ShotDir
    New-Item -ItemType Directory -Force $SaveDir, $ShotDir | Out-Null
    $exe = Join-Path (Get-Location).Path "Builds\Windows\CayTheGioi.exe"
    $a = "-screen-width $W -screen-height $H -screen-fullscreen 0 -ctg-save-dir `"$SaveDir`""
    if ($Utc -gt 0) { $a += " -ctg-utc $Utc" }
    $script:Proc = Start-Process -FilePath $exe -ArgumentList $a -PassThru
    for ($i = 0; $i -lt 40 -and $script:Proc.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 250; $script:Proc.Refresh() }
    Start-Sleep -Seconds 4
    [W32]::MoveWindow($script:Proc.MainWindowHandle, 20, 20, $W, $H, $true) | Out-Null
    [W32]::SetForegroundWindow($script:Proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 800
    return $script:Proc
}
function Get-Win { $r = New-Object W32+RECT; [void][W32]::GetWindowRect($script:Proc.MainWindowHandle, [ref]$r); [pscustomobject]@{ X = $r.L; Y = $r.T; W = $r.R - $r.L; H = $r.B - $r.T } }
function Shot([string]$Name) {
    $w = Get-Win
    $bmp = New-Object System.Drawing.Bitmap $w.W, $w.H
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [void][W32]::PrintWindow($script:Proc.MainWindowHandle, $hdc, 2)
    $g.ReleaseHdc($hdc); $g.Dispose()
    $path = Join-Path $script:ShotDir "$Name.png"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    return $path
}
function Click([int]$ix, [int]$iy, [int]$Count = 1, [int]$DelayMs = 120) {
    $w = Get-Win
    [W32]::SetForegroundWindow($script:Proc.MainWindowHandle) | Out-Null
    [W32]::SetCursorPos($w.X + $ix, $w.Y + $iy) | Out-Null; Start-Sleep -Milliseconds 100
    for ($i = 0; $i -lt $Count; $i++) {
        [W32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 40
        [W32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds $DelayMs
    }
}
function Stop-Player { if ($script:Proc -and -not $script:Proc.HasExited) { [void]$script:Proc.CloseMainWindow(); if (-not $script:Proc.WaitForExit(8000)) { $script:Proc.Kill() } }; return $script:Proc.ExitCode }
