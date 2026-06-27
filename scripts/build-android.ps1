param(
    [ValidateSet("release", "profiling")]
    [string]$Variant = "release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

$validateScript = Join-Path $projectRoot "scripts\Validate-SceneFlow.ps1"
if (Test-Path $validateScript) {
    Write-Host "Running scene flow validation..."
    & $validateScript
    if ($LASTEXITCODE -ne 0) {
        throw "Scene flow validation failed. Fix wiring before building."
    }
}

$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe"
$method = if ($Variant -eq "profiling") {
    "AutomatedAndroidBuild.BuildProfilingAndroidApk"
} else {
    "AutomatedAndroidBuild.BuildLatestStableAndroidApk"
}

$log = Join-Path $projectRoot "Build\Android\unity-android-build.log"
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

Write-Host "Starting Android $Variant build..."
$proc = Start-Process -FilePath $unity -PassThru -Wait -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", $method,
    "-logFile", $log
)

if (-not (Test-Path $log)) {
    throw "Unity log not created: $log"
}

$success = Select-String -Path $log -Pattern "Android APK build succeeded" -Quiet
if ($proc.ExitCode -ne 0 -or -not $success) {
    Get-Content $log -Tail 40
    throw "Unity Android build failed (exit $($proc.ExitCode))"
}

Select-String -Path $log -Pattern "Android APK build succeeded" |
    ForEach-Object { $_.Line }

$apk = Join-Path $projectRoot "Build\Android\MBody-latest-stable.apk"
$obbDevice = Join-Path $projectRoot "Build\Android\main.1.com.CAU.MBody.obb"
$obbLegacy = Join-Path $projectRoot "Build\Android\MBody-latest-stable.main.obb"

if (-not (Test-Path $apk)) { throw "APK missing: $apk" }
if (-not (Test-Path $obbDevice)) {
    if (Test-Path $obbLegacy) {
        Copy-Item $obbLegacy $obbDevice -Force
        Write-Host "Created device OBB name from legacy: $obbDevice"
    } else {
        throw "OBB missing: $obbDevice"
    }
}

$apkMb = [math]::Round((Get-Item $apk).Length / 1MB, 2)
$obbMb = [math]::Round((Get-Item $obbDevice).Length / 1MB, 2)
Write-Host ""
Write-Host "=== Build outputs ===" -ForegroundColor Green
Write-Host "APK: $apk ($apkMb MB)"
Write-Host "OBB: $obbDevice ($obbMb MB)"
Write-Host "Copy OBB to device: Internal/Android/obb/com.CAU.MBody/main.1.com.CAU.MBody.obb"

Write-Host "Log: $log"
