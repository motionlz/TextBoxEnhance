<#
.SYNOPSIS
    Records a window to mp4, and optionally to a gif, using ffmpeg.

.DESCRIPTION
    Captures the region of the desktop a window occupies rather than the window
    itself. gdigrab can read a window by title, but hardware-rendered windows --
    Unity's included -- often come back black that way, while grabbing the same
    rectangle off the desktop always works.

.EXAMPLE
    .\record-clip.ps1
    Ten seconds of the Unity window to Recordings\clip.mp4.

.EXAMPLE
    .\record-clip.ps1 -Seconds 6 -Gif -Out Recordings\wave
    Six seconds, as both mp4 and gif.
#>
param(
    [int]    $Seconds = 10,
    [string] $Window  = "Unity",
    [string] $Out     = "Recordings\clip",
    [int]    $Fps     = 30,
    [switch] $Gif,
    [switch] $FullScreen
)

$ErrorActionPreference = "Stop"

$ffmpeg = Join-Path $env:LOCALAPPDATA `
    "Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0.1-full_build\bin\ffmpeg.exe"

if (-not (Test-Path $ffmpeg)) {
    $found = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if (-not $found) { throw "ffmpeg not found. Install it with: winget install --id Gyan.FFmpeg -e" }
    $ffmpeg = $found.Source
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public struct RECT { public int Left, Top, Right, Bottom; }
public static class Win {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int size);

    // GetWindowRect includes the invisible resize border, which on a maximised window
    // reaches eight pixels past every screen edge -- and gdigrab refuses a capture area
    // that leaves the desktop. The frame bounds DWM reports are what is actually drawn.
    public static RECT VisibleBounds(IntPtr h) {
        RECT r;
        const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        if (DwmGetWindowAttribute(h, DWMWA_EXTENDED_FRAME_BOUNDS, out r, Marshal.SizeOf(typeof(RECT))) == 0)
            return r;
        GetWindowRect(h, out r);
        return r;
    }
}
"@

$ffArgs = @("-y", "-f", "gdigrab", "-framerate", $Fps)

if (-not $FullScreen) {
    $process = Get-Process -Name $Window -ErrorAction SilentlyContinue |
               Where-Object { $_.MainWindowTitle } | Select-Object -First 1

    if (-not $process) { throw "No window found for a process named '$Window'. Use -FullScreen, or pass -Window <processname>." }

    Write-Host "Recording: $($process.MainWindowTitle)"
    [void][Win]::SetForegroundWindow($process.MainWindowHandle)
    Start-Sleep -Milliseconds 400

    $rect = [Win]::VisibleBounds($process.MainWindowHandle)

    # Clip to the desktop as well: a window can still hang off the edge, and gdigrab
    # treats a capture area that does as an error rather than cropping it.
    $screen = [System.Windows.Forms.SystemInformation]::VirtualScreen
    $left   = [Math]::Max($rect.Left, $screen.Left)
    $top    = [Math]::Max($rect.Top,  $screen.Top)
    $right  = [Math]::Min($rect.Right,  $screen.Right)
    $bottom = [Math]::Min($rect.Bottom, $screen.Bottom)

    # h264 wants even dimensions.
    $width  = ($right - $left)  - (($right - $left)  % 2)
    $height = ($bottom - $top)  - (($bottom - $top)  % 2)

    if ($width -lt 2 -or $height -lt 2) { throw "That window is not on screen." }

    Write-Host "Area: ${width}x${height} at ($left,$top)"
    $ffArgs += @("-offset_x", $left, "-offset_y", $top, "-video_size", "${width}x${height}")
}

$mp4 = "$Out.mp4"
New-Item -ItemType Directory -Force -Path (Split-Path $mp4 -Parent) | Out-Null

$ffArgs += @("-t", $Seconds, "-i", "desktop", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-preset", "fast", $mp4)

# No 2>&1 here: Windows PowerShell wraps a native command's stderr in error records
# and reports a failure even when ffmpeg exits cleanly. -loglevel keeps it quiet
# instead.
Write-Host "Recording $Seconds seconds..."
& $ffmpeg -loglevel error @ffArgs
if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed with exit code $LASTEXITCODE" }
Write-Host "Wrote $mp4"

if ($Gif) {
    # Named gifPath, not gif: PowerShell variables are case-insensitive and $Gif is
    # already the switch that got us here.
    $gifPath = "$Out.gif"
    # Two passes: one to work out a palette that suits the clip, one to use it.
    # A gif made without that step bands badly on anything with a gradient in it.
    $filter = "fps=15,scale=900:-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse"
    & $ffmpeg -y -loglevel error -i $mp4 -vf $filter -loop 0 $gifPath
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed to build the gif" }
    Write-Host "Wrote $gifPath"
}
