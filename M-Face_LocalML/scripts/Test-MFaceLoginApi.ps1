# M-Face 로그인 화면 API 검증 (Login.cs AdminLogin 흐름 재현)
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/mface",
    [string]$UserId = "hudit",
    [string]$Password = "hudit"
)

$ErrorActionPreference = "Continue"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Step($name, $script) {
    try {
        $detail = & $script
        Write-Host "[PASS] $name - $detail" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "[FAIL] $name - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "=== M-Face Login API @ $BaseUrl ===" -ForegroundColor Cyan
Write-Host "User: $UserId" -ForegroundColor Cyan

$ok = $true
$ok = (Step "GET /api/users/accounts/id/has/{id}" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts/id/has/$UserId" -UseBasicParsing
    if ($r.StatusCode -ne 200) { throw "HTTP $($r.StatusCode)" }
    "body=$($r.Content)"
}) -and $ok

$exists = (Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts/id/has/$UserId" -UseBasicParsing).Content -eq 'true'

if (-not $exists) {
    $ok = (Step "POST /api/users/sign-in (super-admin)" {
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $session `
            -ContentType "application/json" -Body '{"id":"super-admin","password":"super-admin!@#$"}' -UseBasicParsing
        "HTTP $($r.StatusCode)"
    }) -and $ok

    $ok = (Step "POST /api/users/accounts (회원가입)" {
        $body = (@{ level=1; domain="spike"; id=$UserId; password=$Password; name=$UserId; email="$UserId@test.com"; phone="000" } | ConvertTo-Json -Compress)
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts" -Method POST -WebSession $session `
            -ContentType "application/json" -Body $body -UseBasicParsing
        "HTTP $($r.StatusCode) $($r.Content)"
    }) -and $ok

    $ok = (Step "GET /api/users/sign-out (admin)" {
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-out" -WebSession $session -UseBasicParsing
        "HTTP $($r.StatusCode)"
    }) -and $ok
}

$userSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$seq = 0
$ok = (Step "POST /api/users/sign-in (이용자 로그인)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $userSession `
        -ContentType "application/json" -Body (@{ id=$UserId; password=$Password } | ConvertTo-Json -Compress) -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.seq -le 0) { throw "seq missing: $($r.Content)" }
    $script:seq = $obj.seq
    "HTTP $($r.StatusCode) seq=$seq"
}) -and $ok

$ok = (Step "GET /api/players/me/progress (진행도 동기화)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -WebSession $userSession -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.dayCount -lt 1 -or $obj.itemString.Length -ne 16) { throw "invalid progress: $($r.Content)" }
    "dayCount=$($obj.dayCount) itemString=$($obj.itemString)"
}) -and $ok

$ok = (Step "GET /api/users/sign-out (이용자)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-out" -WebSession $userSession -UseBasicParsing
    "HTTP $($r.StatusCode)"
}) -and $ok

if ($ok) {
    Write-Host "`nRESULT: ALL PASS" -ForegroundColor Green
    exit 0
}
Write-Host "`nRESULT: SOME FAILED" -ForegroundColor Red
exit 1
