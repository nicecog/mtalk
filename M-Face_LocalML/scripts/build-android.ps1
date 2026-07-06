$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

$unityCandidates = @(
    "C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Unity.exe"
)
$unity = $unityCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $unity) { throw "Unity Editor not found." }

$preBuild = Join-Path $projectRoot "scripts\Test-MFacePreBuild.ps1"
if (Test-Path $preBuild) {
    Write-Host "Running pre-build verification..."
    & $preBuild
    if ($LASTEXITCODE -ne 0) {
        throw "Pre-build verification failed."
    }
}

$log = Join-Path $projectRoot "Build\Android\unity-android-build.log"
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

Write-Host "Starting M-Face Android build..."
$proc = Start-Process -FilePath $unity -PassThru -Wait -NoNewWindow -ArgumentList @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "AutomatedAndroidBuild.BuildLatestStableAndroidApk",
    "-logFile", $log
)

if (-not (Test-Path $log)) { throw "Unity log not created: $log" }

$success = Select-String -Path $log -Pattern "Android APK build succeeded" -Quiet
if ($proc.ExitCode -ne 0 -or -not $success) {
    Get-Content $log -Tail 40
    throw "Unity Android build failed (exit $($proc.ExitCode))"
}

$apk = Join-Path $projectRoot "Build\Android\MFace-latest-stable.apk"
if (-not (Test-Path $apk)) { throw "APK missing: $apk" }

$apkMb = [math]::Round((Get-Item $apk).Length / 1MB, 2)
Write-Host ""
Write-Host "=== Build output ===" -ForegroundColor Green
Write-Host "APK: $apk ($apkMb MB)"
Write-Host "Package: com.Spike.MFace"
Write-Host "Log: $log"
