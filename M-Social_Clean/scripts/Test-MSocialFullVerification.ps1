# M-Social 전수 검증 — API + 빌드 설정 + 에셋 구조
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/msocial",
    [string]$UserId = "hudit",
    [string]$Password = "hudit",
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Continue"
$pass = 0; $fail = 0

function Report($name, $ok, $detail) {
    if ($ok) { $script:pass++; Write-Host "[PASS] $name - $detail" -ForegroundColor Green }
    else { $script:fail++; Write-Host "[FAIL] $name - $detail" -ForegroundColor Red }
}

Write-Host "=== M-Social Full Verification ===" -ForegroundColor Cyan

& "$PSScriptRoot\Test-MSocialLoginApi.ps1" -BaseUrl $BaseUrl -UserId $UserId -Password $Password
if ($LASTEXITCODE -ne 0) { $fail++ } else { $pass += 6 }

& "$PSScriptRoot\Test-MSocialPreBuild.ps1" -BaseUrl $BaseUrl -ProjectRoot $ProjectRoot
if ($LASTEXITCODE -ne 0) { $fail++ }

$idx = Join-Path $ProjectRoot "Assets\StreamingAssets\Movie\movie_index.json"
if (Test-Path $idx) {
    $j = Get-Content $idx -Raw | ConvertFrom-Json
    Report "movie_index.json" ($j.stage1.Count -eq 32 -and $j.stage2.Count -eq 32) "stage1=$($j.stage1.Count) stage2=$($j.stage2.Count) intro=$($j.intro.Count)"
} else { Report "movie_index.json" $false "missing" }

$streamCount = (Get-ChildItem (Join-Path $ProjectRoot "Assets\StreamingAssets") -Recurse -Filter "*.mp4").Count
Report "StreamingAssets MP4" ($streamCount -ge 60) "count=$streamCount"

$assetMovieCount = (Get-ChildItem (Join-Path $ProjectRoot "Assets\Movie") -Recurse -Filter "*.mp4" -ErrorAction SilentlyContinue).Count
Report "Assets/Movie MP4 removed" ($assetMovieCount -eq 0) "remaining=$assetMovieCount (should be 0 for lazy load)"

foreach ($f in @("MSocialMovieLibrary.cs","MSocialVideoPlayerHelper.cs","MSocialIntroMoviePlayer.cs")) {
    Report $f (Test-Path (Join-Path $ProjectRoot "Assets\$f")) "on-demand video"
}

$ps = Get-Content (Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset") -Raw
Report "ARMv7 only" ($ps -match 'AndroidTargetArchitectures: 1') "IL2CPP armeabi-v7a"
Report "versionCode 17" ($ps -match 'AndroidBundleVersionCode: 17') "v1.29"

foreach ($f in @("MSocialVideoRtOptimizer.cs")) {
    Report $f (Test-Path (Join-Path $ProjectRoot "Assets\$f")) "low-end video RT"
}

foreach ($f in @("MSocialSceneRouter.cs","MSocialPageRegistry.cs","MSocialSceneBootstrap.cs","MSocialStageCatalog.cs")) {
    Report $f (Test-Path (Join-Path $ProjectRoot "Assets\$f")) "scene split"
}

Write-Host ""
Write-Host "=== Summary: PASS=$pass FAIL=$fail ===" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
exit 0
