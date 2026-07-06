$ErrorActionPreference = "Stop"
$adb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adb)) { throw "adb not found: $adb" }

$serial = "R9WR611YAAJ"
$devices = & $adb devices | Select-String "device$"
if ($devices.Count -eq 0) { throw "No Android device connected." }

Write-Host "M-Face logcat (tag: MFaceLocalML). Ctrl+C to stop." -ForegroundColor Cyan
Write-Host "  adb -s $serial logcat -s MFaceLocalML:I MFaceLocalML:W MFaceLocalML:E"
Write-Host ""

if ($args -contains "-c") {
    & $adb -s $serial logcat -c
    Write-Host "Log buffer cleared." -ForegroundColor Yellow
}

& $adb -s $serial logcat -s "MFaceLocalML:I" "MFaceLocalML:W" "MFaceLocalML:E"
