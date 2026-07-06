# M-Social Clean pre-build verification
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/msocial",
    [string]$UserId = "hudit",
    [string]$Password = "hudit",
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Continue"
$pass = 0
$fail = 0

function Report($name, $ok, $detail) {
    if ($ok) {
        $script:pass++
        Write-Host "[PASS] $name - $detail" -ForegroundColor Green
    }
    else {
        $script:fail++
        Write-Host "[FAIL] $name - $detail" -ForegroundColor Red
    }
}

Write-Host "=== M-Social Pre-Build Verification ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Server:  $BaseUrl"
Write-Host ""

$flowMgr = Join-Path $ProjectRoot "Assets\FlowManager.cs"
$login = Join-Path $ProjectRoot "Assets\Login.cs"
$bootstrap = Join-Path $ProjectRoot "Assets\Scenes\MSocial_Bootstrap.unity"
$stage1 = Join-Path $ProjectRoot "Assets\Scenes\MSocial_Stage1.unity"
$stage2 = Join-Path $ProjectRoot "Assets\Scenes\MSocial_Stage2.unity"
$manifest = Join-Path $ProjectRoot "Assets\Plugins\Android\AndroidManifest.xml"
$apk = Join-Path $ProjectRoot "Build\Android\MSocial-latest-stable.apk"

Report "MSocialApiClient.cs" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialApiClient.cs")) "API client"
Report "MSocialLoginAlert.cs" (Test-Path (Join-Path $ProjectRoot "Assets\Login\MSocialLoginAlert.cs")) "login alerts"

if (Test-Path $flowMgr) {
    $fm = Get-Content $flowMgr -Raw
    Report "FlowManager homePageId" ($fm -match 'homePageId = MSocialPageId.Stage1Start') "page id"
    Report "FlowManager SceneRouter" ($fm -match 'MSocialSceneRouter') "split scenes"
    Report "FlowManager bootstrap show" ($fm -match 'ShowBootstrapPage') "white-screen fix"
    Report "FlowManager Start reset" ($fm -match 'void Start\(\)') "init order fix"
}
else { Report "FlowManager.cs" $false "missing" }

if (Test-Path $login) {
    $lg = Get-Content $login -Raw
    Report "Login uses MSocialApiClient" ($lg -match 'MSocialApiClient') "refactored"
    Report "Login -> Stage1SelectMap" ($lg -match 'GoToPage\(MSocialPageId\.Stage1SelectMap\)') "direct map"
    Report "StageWireup" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialStageWireup.cs")) "cross-scene refs"
    Report "Login error guards" ($lg -match 'loginBusy') "busy guard"
    Report "fileUpdate CSV guards" ($lg -match 'foundCsv') "safe CSV parse"
}
else { Report "Login.cs" $false "missing" }

Report "MSocial_Bootstrap.unity" (Test-Path $bootstrap) "boot scene"
Report "MSocial_Stage1.unity" (Test-Path $stage1) "stage1 scene"
Report "MSocial_Stage2.unity" (Test-Path $stage2) "stage2 scene"
    Report "MSocialSceneRouter.cs" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialSceneRouter.cs")) "router"
    Report "MSocialVideoRtOptimizer" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialVideoRtOptimizer.cs")) "low-end RT only"
    Report "Canvas design 2800" ((Get-Content (Join-Path $ProjectRoot "Assets\MSocialDeviceProfile.cs") -Raw) -match '2800f, 1752f')
    $appSvc = Join-Path $ProjectRoot "Assets\MSocialAppServices.cs"
    if (Test-Path $appSvc) {
        $as = Get-Content $appSvc -Raw
        Report "Login not DDOL" ($as -notmatch 'DontDestroyOnLoad\(login') "canvas UI fix"
    }

$buildSettings = Join-Path $ProjectRoot "ProjectSettings\EditorBuildSettings.asset"
if (Test-Path $buildSettings) {
    $bs = Get-Content $buildSettings -Raw
    Report "Build Bootstrap first" ($bs -match 'MSocial_Bootstrap') "scene order"
    Report "Build Stage1" ($bs -match 'MSocial_Stage1') "additive"
    Report "Build Stage2" ($bs -match 'MSocial_Stage2') "additive"
}

if (Test-Path (Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset")) {
    $ps = Get-Content (Join-Path $ProjectRoot "ProjectSettings\ProjectSettings.asset") -Raw
    Report "targetSdk 33" ($ps -match 'AndroidTargetSdkVersion: 33') "Android 11+ security warning fix"
    Report "PageMoviePlayer" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialPageMoviePlayer.cs")) "stage intro video"
    Report "ScenePlayer play fix" ((Get-Content (Join-Path $ProjectRoot "Assets\ScenePlayer.cs") -Raw) -match 'PlayPrepared')
    Report "Android video URL encode" ((Get-Content (Join-Path $ProjectRoot "Assets\MSocialVideoPaths.cs") -Raw) -match 'EncodeAndroidStreamingUrl')
    Report "versionCode >= 17" ($ps -match 'AndroidBundleVersionCode: 17') "v1.29 video cache"
    Report "video cache" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialVideoCache.cs")) "android file cache"
    Report "ARMv7 only" ($ps -match 'AndroidTargetArchitectures: 1') "Tab A 32-bit optimized"
}

if (Test-Path $manifest) {
    $mf = Get-Content $manifest -Raw
    Report "AndroidManifest launcher" ($mf -match 'MAIN' -and $mf -match 'LAUNCHER') "UnityPlayerActivity"
    Report "MSocialDeviceProfile" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialDeviceProfile.cs")) "low-memory profile"
    Report "MSocialStartupLog" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialStartupLog.cs")) "startup diagnostics"
    Report "MSocialMovieLibrary" (Test-Path (Join-Path $ProjectRoot "Assets\MSocialMovieLibrary.cs")) "on-demand video"
    Report "movie_index.json" (Test-Path (Join-Path $ProjectRoot "Assets\StreamingAssets\Movie\movie_index.json")) "streaming catalog"
}
else { Report "AndroidManifest.xml" $false "missing" }

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
try {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/health" -UseBasicParsing -TimeoutSec 15
    Report "GET /api/health" ($r.StatusCode -eq 200) "HTTP $($r.StatusCode)"
}
catch { Report "GET /api/health" $false $_.Exception.Message }

try {
    $body = (@{ id = $UserId; password = $Password } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $session `
        -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 15
    $obj = $r.Content | ConvertFrom-Json
    Report "POST sign-in" ($obj.seq -gt 0) "seq=$($obj.seq)"
}
catch { Report "POST sign-in" $false $_.Exception.Message }

if (Test-Path $apk) {
    $aapt = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools" `
        -Recurse -Filter "aapt.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($aapt) {
        $badging = & $aapt.FullName dump badging $apk 2>$null
        $abi = $badging | Where-Object { $_ -match 'native-code' }
        Report "Previous APK ABI" ($abi -match 'armeabi-v7a' -and $abi -notmatch 'arm64-v8a') ($abi -replace '^\s+','')
    }
}

Write-Host ""
Write-Host "=== Summary: PASS=$pass FAIL=$fail ===" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
exit 0
