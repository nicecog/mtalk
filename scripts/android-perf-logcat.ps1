param(
    [string]$AdbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    [string]$ApkPath = "Build\Android\MBody-latest-stable.apk",
    [int]$DurationSec = 120,
    [switch]$Install,
    [switch]$ProfilingApk
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

if ($ProfilingApk) {
    $ApkPath = "Build\Android\MBody-profiling-dev.apk"
}

if (-not (Test-Path $AdbPath)) {
    throw "adb not found: $AdbPath"
}

$devices = & $AdbPath devices | Select-String "device$"
if (-not $devices) {
    throw "No Android device connected. Enable USB debugging and reconnect."
}

if ($Install) {
    if (-not (Test-Path $ApkPath)) {
        throw "APK not found: $ApkPath"
    }
    Write-Host "Installing $ApkPath ..."
    & $AdbPath install -r $ApkPath
}

$logFile = Join-Path $projectRoot ("Build\Android\perf-logcat-{0:yyyyMMdd-HHmmss}.txt" -f (Get-Date))
New-Item -ItemType Directory -Force -Path (Split-Path $logFile) | Out-Null

Write-Host "Capturing logcat for $DurationSec sec -> $logFile"
Write-Host "Filter: PerformanceManager | PerfStats | Unity"

& $AdbPath logcat -c
$job = Start-Job -ScriptBlock {
    param($adb, $out)
    & $adb logcat -v time Unity:* PerformanceManager:* PerfStats:* *:S 2>&1 |
        Tee-Object -FilePath $out
} -ArgumentList $AdbPath, $logFile

Start-Sleep -Seconds $DurationSec
Stop-Job $job -ErrorAction SilentlyContinue
Remove-Job $job -Force -ErrorAction SilentlyContinue

Write-Host "Done. Log: $logFile"
Get-Content $logFile -ErrorAction SilentlyContinue |
    Select-String "PerformanceManager|PerfStats" |
    Select-Object -Last 20
