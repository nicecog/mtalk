param(
    [string]$AdbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    [int]$ObbPushRetries = 5
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$apk = "Build\Android\MBody-latest-stable.apk"
$obb = if (Test-Path "Build\Android\main.1.com.CAU.MBody.obb") {
    "Build\Android\main.1.com.CAU.MBody.obb"
} else {
    "Build\Android\MBody-latest-stable.main.obb"
}

if (-not (Test-Path $AdbPath)) { throw "adb not found: $AdbPath" }
if (-not (Test-Path $apk)) { throw "APK missing: $apk" }
if (-not (Test-Path $obb)) { throw "OBB missing: $obb" }

function Wait-AdbDevice([string]$Serial, [int]$TimeoutSec = 120) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        & $AdbPath kill-server | Out-Null
        Start-Sleep -Seconds 1
        & $AdbPath start-server | Out-Null
        Start-Sleep -Seconds 1
        $line = (& $AdbPath devices | Select-String "$Serial\s+device")
        if ($line) { return $true }
        Write-Host "Waiting for $Serial (USB reconnect / allow debugging)..."
        Start-Sleep -Seconds 5
    }
    return $false
}

function Get-RemoteObbSize([string]$Serial, [string]$RemotePath) {
    $raw = & $AdbPath -s $Serial shell "ls -l `"$RemotePath`" 2>/dev/null"
    if (-not $raw) { return -1 }
    $line = ($raw | Select-Object -First 1).ToString().Trim()
    if ($line -match '\s(\d+)\s+\d{4}-\d{2}-\d{2}') {
        return [int64]$Matches[1]
    }
    return -1
}

if (-not (Wait-AdbDevice "R9WR611YAAJ")) {
    throw "Tab A8 not online. Re-plug USB cable and allow USB debugging."
}

$serial = "R9WR611YAAJ"
$pkg = "com.CAU.MBody"
$obbName = Split-Path -Leaf $obb
$obbRemote = "/sdcard/Android/obb/$pkg/$obbName"
$expectedSize = (Get-Item $obb).Length

Write-Host "Installing APK to $serial ..."
& $AdbPath -s $serial install -r $apk
if ($LASTEXITCODE -ne 0) { throw "adb install failed ($LASTEXITCODE)" }

Write-Host "Ensuring OBB directory..."
& $AdbPath -s $serial shell "mkdir -p /sdcard/Android/obb/$pkg"

$remoteSize = Get-RemoteObbSize $serial $obbRemote
if ($remoteSize -eq $expectedSize) {
    Write-Host "OBB already present ($remoteSize bytes) — skip push"
} else {
    if ($remoteSize -gt 0) {
        Write-Host "Removing incomplete OBB ($remoteSize bytes, expected $expectedSize)..."
        & $AdbPath -s $serial shell "rm -f `"$obbRemote`""
    }

    $pushed = $false
    for ($i = 1; $i -le $ObbPushRetries; $i++) {
        Write-Host "Pushing OBB attempt $i/$ObbPushRetries ($expectedSize bytes)..."
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & $AdbPath -s $serial push $obb $obbRemote 2>&1 | ForEach-Object { Write-Host $_ }
        $ErrorActionPreference = $prevEap

        Start-Sleep -Seconds 2
        if (-not (Wait-AdbDevice $serial 60)) {
            Write-Host "Device offline after push — reconnect USB and retrying..."
            continue
        }

        $remoteSize = Get-RemoteObbSize $serial $obbRemote
        Write-Host "Remote OBB size: $remoteSize"
        if ($remoteSize -eq $expectedSize) {
            $pushed = $true
            break
        }
    }

    if (-not $pushed) {
        throw "OBB push failed. Expected $expectedSize bytes on device. Re-plug USB and run again."
    }
}

Write-Host "OK: APK installed + OBB verified ($obbName, $expectedSize bytes)"
