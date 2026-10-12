. "$PSScriptRoot\ui_helpers.ps1"
Add-Type @"
using System; using System.Runtime.InteropServices;
public class PM {
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] i, int size);
  [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx, dy; public uint data, flags, time; public UIntPtr extra; }
  [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public MOUSEINPUT mi; }
}
"@
$root = (Get-Location).Path
$d = Join-Path $root 'Evidence\T09\StandaloneQA\ui_run'
if (Test-Path $d) { Remove-Item $d -Recurse -Force }
$p = Start-Player -SaveDir "$d\save" -ShotDir "$d\shots"
$h = $p.MainWindowHandle
function Pm([uint32]$m, [int]$cx, [int]$cy, [int]$wp = 0) { $l = [IntPtr](($cy -shl 16) -bor ($cx -band 0xFFFF)); [void][PM]::PostMessage($h, $m, [IntPtr]$wp, $l) }
# client coords: anh co thanh tieu de 38px? client x offset ~9, y offset ~38 -> nut Cai dat o anh (1075,80) => client (1066,42)
$cx = 1066; $cy = 42
Pm 0x0200 $cx $cy; Start-Sleep -Milliseconds 100
Pm 0x0201 $cx $cy 1; Start-Sleep -Milliseconds 80
Pm 0x0202 $cx $cy 0; Start-Sleep -Milliseconds 800
Shot 'ui_postmessage_settings' | Out-Null
"exit=" + (Stop-Player)
