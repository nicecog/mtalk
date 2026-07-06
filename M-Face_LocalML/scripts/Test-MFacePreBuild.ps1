# M-Face LocalML pre-build verification (automated)
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/mface",
    [string]$UserId = "hudit",
    [string]$Password = "hudit",
    [string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = "Continue"
$pass = 0
$fail = 0
$warn = 0

function Report($name, $ok, $detail, $isWarn = $false) {
    if ($ok) {
        $script:pass++
        Write-Host "[PASS] $name - $detail" -ForegroundColor Green
    }
    elseif ($isWarn) {
        $script:warn++
        Write-Host "[WARN] $name - $detail" -ForegroundColor Yellow
    }
    else {
        $script:fail++
        Write-Host "[FAIL] $name - $detail" -ForegroundColor Red
    }
}

Write-Host "=== M-Face LocalML Pre-Build Verification ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Server:  $BaseUrl"
Write-Host ""

# --- Project files ---
$tflite = Join-Path $ProjectRoot "Assets\StreamingAssets\ML\emotion-ferplus-8.tflite"
$onnx = Join-Path $ProjectRoot "Assets\MLModels\emotion-ferplus-8.onnx"
$flowMgr = Join-Path $ProjectRoot "Assets\FlowManager.cs"
$scene = Join-Path $ProjectRoot "Assets\Scenes\MainScene.unity"
$apk = Join-Path $ProjectRoot "Build\Android\MFace-latest-stable.apk"

Report "TFLite model (Android)" (Test-Path $tflite) $(if (Test-Path $tflite) { "{0:N0} bytes" -f (Get-Item $tflite).Length } else { "missing" })
Report "ONNX model (Editor)" (Test-Path $onnx) $(if (Test-Path $onnx) { "{0:N0} bytes" -f (Get-Item $onnx).Length } else { "missing" })
Report "MFaceProgressApi.cs" (Test-Path (Join-Path $ProjectRoot "Assets\MFaceProgressApi.cs")) "progress API client"
Report "MFaceCsvStore.cs" (Test-Path (Join-Path $ProjectRoot "Assets\MFaceCsvStore.cs")) "local CSV mirror"
Report "WebCam in-memory analyze" ((Get-Content (Join-Path $ProjectRoot "Assets\WebCam.cs") -Raw) -match 'AnalyzeTexture') "no JPG round-trip"
Report "BlazeFace always init" ((Get-Content (Join-Path $ProjectRoot "Assets\LocalML\MFaceLocalEmotionAnalyzer.cs") -Raw) -match 'BlazeFacePredictor\.Create') "face detector on startup"

if (Test-Path $flowMgr) {
    $fm = Get-Content $flowMgr -Raw
    Report "FlowManager HomePage field" ($fm -match 'public GameObject HomePage') "HomePage defined"
    Report "FlowManager HomeReset uses HomePage" ($fm -match 'GoToPage\(HomePage') "HomeReset -> HomePage"
}
else { Report "FlowManager.cs" $false "file missing" }

if (Test-Path $scene) {
    $sc = Get-Content $scene -Raw
    Report "Scene HomePage wired" ($sc -match 'HomePage: \{fileID: 199373684\}') "4. Stage1Start (199373684)"
    Report "Scene FirstPage is IntroLogo" ($sc -match 'FirstPage: \{fileID: 584831058\}') "1. IntroLogo (app start only)"
    Report "Login ButtonFlow -> IntroMovie" ($sc -match 'SelfPage: \{fileID: 1584764534\}[\s\S]{0,80}NextPage: \{fileID: 807675433\}') "LoginPage -> IntroMovie"
}
else { Report "MainScene.unity" $false "file missing" }

Report "Previous APK exists" (Test-Path $apk) $(if (Test-Path $apk) { "{0:N0} bytes (will rebuild)" -f (Get-Item $apk).Length } else { "none yet" }) $true

# --- Server API ---
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

try {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/health" -UseBasicParsing -TimeoutSec 15
    Report "GET /api/health" ($r.StatusCode -eq 200) "HTTP $($r.StatusCode) $($r.Content)"
}
catch { Report "GET /api/health" $false $_.Exception.Message }

try {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts/id/has/$UserId" -UseBasicParsing -TimeoutSec 15
    Report "GET has/$UserId" ($r.StatusCode -eq 200) "body=$($r.Content)"
}
catch { Report "GET has/$UserId" $false $_.Exception.Message }

try {
    $body = (@{ id = $UserId; password = $Password } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $session `
        -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 15
    $obj = $r.Content | ConvertFrom-Json
    $seq = $obj.seq
    Report "POST sign-in (user)" ($seq -gt 0) "HTTP $($r.StatusCode) seq=$seq"
}
catch { Report "POST sign-in (user)" $false $_.Exception.Message; $seq = 0 }

if ($seq -gt 0) {
    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -WebSession $session -UseBasicParsing -TimeoutSec 15
        $obj = $r.Content | ConvertFrom-Json
        Report "GET /api/players/me/progress" ($obj.dayCount -ge 1 -and $obj.itemString.Length -eq 16) "dayCount=$($obj.dayCount) item=$($obj.itemString)"
    }
    catch { Report "GET /api/players/me/progress" $false $_.Exception.Message }

    try {
        $body = (@{ dayCount = 1; itemString = "FFFFFFFFFFFFFFFF" } | ConvertTo-Json -Compress)
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -Method PUT -WebSession $session `
            -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 15
        Report "PUT /api/players/me/progress" ($r.StatusCode -ge 200 -and $r.StatusCode -lt 300) "HTTP $($r.StatusCode)"
    }
    catch { Report "PUT /api/players/me/progress" $false $_.Exception.Message }

    try {
        $body = (@{ logDate = (Get-Date -Format 'yyyy-MM-dd'); logTime = (Get-Date -Format 'HH.mm.ss'); logBody = "{Stage:1;Point:(1/1);Face:0}" } | ConvertTo-Json -Compress)
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/activity-logs" -Method POST -WebSession $session `
            -ContentType "application/json" -Body $body -UseBasicParsing -TimeoutSec 15
        Report "POST /api/players/me/activity-logs" ($r.StatusCode -ge 200 -and $r.StatusCode -lt 300) "HTTP $($r.StatusCode)"
    }
    catch { Report "POST /api/players/me/activity-logs" $false $_.Exception.Message }

    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-out" -WebSession $session -UseBasicParsing -TimeoutSec 15
        Report "GET sign-out" ($r.StatusCode -eq 200) "HTTP $($r.StatusCode)"
    }
    catch { Report "GET sign-out" $false $_.Exception.Message }
}

# --- APK asset check (if APK present) ---
if (Test-Path $apk) {
    $zipList = & jar.exe tf $apk 2>$null
    if ($LASTEXITCODE -eq 0) {
        $hasTflite = $zipList | Where-Object { $_ -match 'emotion-ferplus-8\.tflite' }
        Report "APK contains TFLite model" ($null -ne $hasTflite) $(if ($hasTflite) { $hasTflite } else { "not found in APK" })
    }
    else {
        Report "APK jar listing" $false "jar.exe unavailable" $true
    }

    $aapt = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools" `
        -Recurse -Filter "aapt.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($aapt) {
        $badging = & $aapt.FullName dump badging $apk 2>$null
        $launch = $badging | Where-Object { $_ -match 'launchable-activity' }
        Report "APK has launcher activity" ($null -ne $launch) $(if ($launch) { ($launch -replace '^\s+','') } else { "no launchable-activity" })
    }
    else {
        Report "APK launcher check" $true "aapt not found; skipped" $true
    }
}

Write-Host ""
Write-Host "=== Summary: PASS=$pass FAIL=$fail WARN=$warn ===" -ForegroundColor Cyan
if ($fail -gt 0) { exit 1 }
exit 0
