param(
    [switch]$SkipApi,
    [switch]$SkipMp4,
    [string]$AdbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$results = New-Object System.Collections.Generic.List[object]
function Add-Check($name, $pass, $detail) {
    $mark = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] $name - $detail"
    $results.Add([PSCustomObject]@{ Test = $name; Pass = $pass; Detail = $detail }) | Out-Null
}

Write-Host "========== Pre-Install Deep Verification ==========" -ForegroundColor Cyan

# 1) Scene flow
Write-Host "`n== Scene flow =="
python scripts/validate-scene-flow.py .
$sceneOk = $LASTEXITCODE -eq 0
Add-Check "scene-flow-validation" $sceneOk "validate-scene-flow.py exit=$LASTEXITCODE"

python scripts/validate-scene-pack-deep.py .
$packOk = $LASTEXITCODE -eq 0
Add-Check "scene-pack-deep" $packOk "cross-pack transitions + build scenes"

# 2) Build artifacts
Write-Host "`n== Build artifacts =="
$apk = "Build\Android\MBody-latest-stable.apk"
$obb = "Build\Android\MBody-latest-stable.main.obb"
$apkOk = Test-Path $apk
$obbOk = Test-Path $obb
if ($apkOk) {
    $apkMb = [math]::Round((Get-Item $apk).Length / 1MB, 2)
    Add-Check "apk-exists" $true "$apk ($apkMb MB)"
} else { Add-Check "apk-exists" $false "missing $apk" }
if ($obbOk) {
    $obbMb = [math]::Round((Get-Item $obb).Length / 1MB, 2)
    Add-Check "obb-exists" $true "$obb ($obbMb MB)"
} else { Add-Check "obb-exists" $false "missing $obb" }

# 3) MP4 assets
if (-not $SkipMp4) {
    Write-Host "`n== Dance MP4 =="
    & "$PSScriptRoot\verify-dance-mp4-swap.ps1"
    $mp4SwapOk = $LASTEXITCODE -eq 0
    if ($null -eq $LASTEXITCODE) { $mp4SwapOk = $true }
    Add-Check "dance-mp4-swap" $mp4SwapOk "count/size check"

    if (Test-Path "$PSScriptRoot\verify-mp4-integrity.ps1") {
        & "$PSScriptRoot\verify-mp4-integrity.ps1"
        $mp4IntOk = $LASTEXITCODE -eq 0
        if ($null -eq $LASTEXITCODE) { $mp4IntOk = $true }
        Add-Check "mp4-integrity" $mp4IntOk "ffprobe moov check"
    }
}

# 4) API
if (-not $SkipApi) {
    Write-Host "`n== API (remote server) =="
    $base = "http://hudit.cafe24.com:8080/mbody"
    try {
        $health = Invoke-RestMethod -Uri "$base/api/health" -TimeoutSec 15
        Add-Check "api-health" ($health.status -eq "ok") ($health | ConvertTo-Json -Compress)
    } catch {
        Add-Check "api-health" $false $_.Exception.Message
    }

    try {
        $has = Invoke-WebRequest -Uri "$base/api/users/accounts/id/has/s_hudit1" -UseBasicParsing -TimeoutSec 15
        Add-Check "api-has-account" ($has.StatusCode -eq 200) $has.Content.Trim()
    } catch {
        Add-Check "api-has-account" $false $_.Exception.Message
    }

    $loginBody = '{"id":"s_hudit1","password":"qTNCkgvLmxb3"}'
    try {
        $login = Invoke-WebRequest -Uri "$base/api/users/login" -Method POST -Body $loginBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 15
        $parsed = $login.Content | ConvertFrom-Json
        Add-Check "api-login" ($parsed.seq -gt 0) "seq=$($parsed.seq)"
    } catch {
        Add-Check "api-login" $false $_.Exception.Message
    }

    try {
        $bad = Invoke-WebRequest -Uri "$base/api/users/login" -Method POST -Body '{"id":"s_hudit1","password":"wrong"}' -ContentType "application/json" -UseBasicParsing -TimeoutSec 15
        Add-Check "api-login-reject-bad-pw" $false "unexpected 200"
    } catch {
        $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        Add-Check "api-login-reject-bad-pw" ($code -ge 400) "HTTP $code"
    }
}

# 5) Device readiness (optional — not required for PC-side gate)
Write-Host "`n== Device (optional) =="
if (Test-Path $AdbPath) {
    $dev = & $AdbPath devices | Select-String "device$"
    if ($dev) {
        Add-Check "adb-device" $true $dev.Line
    } else {
        Write-Host "[SKIP] adb-device - no USB device (install test later)" -ForegroundColor Yellow
    }
} else {
    Write-Host "[SKIP] adb-device - adb not found" -ForegroundColor Yellow
}

# 6) Unity editor API verify (batchmode, no device)
Write-Host "`n== Unity compile + editor API =="
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe"
$unityLog = Join-Path $projectRoot "Build\Android\unity-preinstall-api.log"
if (Test-Path $unity) {
    $proc = Start-Process -FilePath $unity -PassThru -Wait -NoNewWindow -ArgumentList @(
        "-batchmode", "-nographics", "-quit",
        "-projectPath", $projectRoot,
        "-executeMethod", "Cafe24ApiVerification.Run",
        "-logFile", $unityLog
    )
    $apiPass = Select-String -Path $unityLog -Pattern "RESULT: ALL PASS" -Quiet
    Add-Check "unity-editor-api" ($proc.ExitCode -eq 0 -and $apiPass) "exit=$($proc.ExitCode)"
} else {
    Add-Check "unity-editor-api" $false "Unity editor not found"
}

# Summary
Write-Host "`n========== Summary ==========" -ForegroundColor Cyan
$passed = @($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host "$passed / $total checks passed"
$results | Format-Table -AutoSize
if ($passed -lt $total) { exit 1 }
exit 0
