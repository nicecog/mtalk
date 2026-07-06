param(
    [string]$AdbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    [string]$ServerLanIp = "hudit.cafe24.com",
    [int]$ServerPort = 8080,
    [string]$ServicePath = "/mbody",
    [int]$DeviceWaitSec = 180,
    [int]$LogcatSec = 90,
    [switch]$SkipInstall,
    [switch]$ProfilingApk
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$baseUrl = "http://${ServerLanIp}:${ServerPort}${ServicePath}"
$apk = if ($ProfilingApk) { "Build\Android\MBody-profiling-dev.apk" } else { "Build\Android\MBody-latest-stable.apk" }
$obb = if ($ProfilingApk -and (Test-Path "Build\Android\MBody-profiling-dev.main.obb")) {
    "Build\Android\MBody-profiling-dev.main.obb"
} else {
    "Build\Android\MBody-latest-stable.main.obb"
}
$package = "com.CAU.MBody"
$activity = "com.unity3d.player.UnityPlayerActivity"
$logFile = Join-Path $projectRoot ("Build\Android\device-api-test-{0:yyyyMMdd-HHmmss}.txt" -f (Get-Date))

function Write-Step($msg) { Write-Host ""; Write-Host "==> $msg" -ForegroundColor Cyan }
function Add-Result($name, $pass, $detail) {
    $mark = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] $name - $detail"
    [PSCustomObject]@{ Test = $name; Pass = $pass; Detail = $detail }
}

if (-not (Test-Path $AdbPath)) { throw "adb not found: $AdbPath" }

Write-Step "Waiting for Android device (max ${DeviceWaitSec}s)"
$deadline = (Get-Date).AddSeconds($DeviceWaitSec)
$serial = $null
while ((Get-Date) -lt $deadline) {
    $lines = & $AdbPath devices | Select-String "device$"
    if ($lines) {
        $serial = ($lines[0].Line -split "\s+")[0]
        break
    }
    Start-Sleep -Seconds 3
}
if (-not $serial) { throw "No Android device connected after ${DeviceWaitSec}s. Enable USB debugging." }
Write-Host "Device: $serial"

$results = New-Object System.Collections.Generic.List[object]

Write-Step "PC server health"
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/health" -TimeoutSec 5
    $results.Add((Add-Result "pc-server-health" ($health.status -eq "ok") ($health | ConvertTo-Json -Compress))) | Out-Null
} catch {
    $results.Add((Add-Result "pc-server-health" $false $_.Exception.Message)) | Out-Null
    throw "Local server not reachable at $baseUrl"
}

Write-Step "Device -> PC network (adb shell curl/wget)"
$netOk = $false
$netDetail = ""
foreach ($cmd in @(
    "curl -sS -m 8 $baseUrl/api/health",
    "wget -qO- --timeout=8 $baseUrl/api/health",
    "toybox wget -qO- --timeout=8 $baseUrl/api/health"
)) {
    $out = & $AdbPath -s $serial shell $cmd 2>&1
    $text = ($out | Out-String).Trim()
    if ($text -match "ok") { $netOk = $true; $netDetail = $text; break }
    $netDetail = $text
}
$results.Add((Add-Result "device-network-health" $netOk $netDetail)) | Out-Null

if (-not $SkipInstall) {
    Write-Step "Install APK + OBB"
    if (-not (Test-Path $apk)) { throw "APK missing: $apk (run build-android.ps1 first)" }
    if (-not (Test-Path $obb)) { throw "OBB missing: $obb" }
    & $AdbPath -s $serial install-multiple -r $apk $obb
    if ($LASTEXITCODE -ne 0) { throw "install-multiple failed ($LASTEXITCODE)" }
    $results.Add((Add-Result "install-apk-obb" $true "$apk + $obb")) | Out-Null
}

Write-Step "Launch app + capture logcat (${LogcatSec}s)"
New-Item -ItemType Directory -Force -Path (Split-Path $logFile) | Out-Null
& $AdbPath -s $serial logcat -c
& $AdbPath -s $serial shell am force-stop $package
Start-Sleep -Seconds 1
& $AdbPath -s $serial shell am start -n "$package/$activity" | Out-Null

$logJob = Start-Job -ScriptBlock {
    param($adb, $serial, $out)
    & $adb -s $serial logcat -v time Unity:* JsonRequest:* PerformanceManager:* PerfStats:* *:S 2>&1 |
        Tee-Object -FilePath $out
} -ArgumentList $AdbPath, $serial, $logFile

Write-Host "Manual step: on device, enter a test ID on login screen and tap login."
Write-Host "Watching logcat for JsonRequest / login / upload messages..."
Start-Sleep -Seconds $LogcatSec
Stop-Job $logJob -ErrorAction SilentlyContinue
Remove-Job $logJob -Force -ErrorAction SilentlyContinue

$logText = ""
if (Test-Path $logFile) { $logText = Get-Content $logFile -Raw -ErrorAction SilentlyContinue }

$baseUrlSeen = $logText -match [regex]::Escape($baseUrl)
$loginOk = $logText -match "Form upload complete! : 200" -or $logText -match '"seq"'
$perfReady = $logText -match "Benchmark complete|Loaded cached tier"
$perfStats = $logText -match "\[PerfStats\]"

$results.Add((Add-Result "logcat-base-url" $baseUrlSeen "customUrl $baseUrl")) | Out-Null
$results.Add((Add-Result "logcat-login-200" $loginOk "grep login response in log")) | Out-Null
$results.Add((Add-Result "logcat-performance" $perfReady "PerformanceManager init")) | Out-Null
$results.Add((Add-Result "logcat-perfstats" $perfStats "runtime stats lines")) | Out-Null

Write-Step "Summary"
$passed = @($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host "$passed / $total checks passed"
Write-Host "Log: $logFile"

$results | Format-Table -AutoSize
if ($passed -lt $total) { exit 1 }
exit 0
