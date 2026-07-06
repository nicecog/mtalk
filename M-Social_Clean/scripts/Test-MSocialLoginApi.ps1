# M-Social 로그인 API 검증 (Login.cs AdminLogin 흐름 재현)
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/msocial",
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

Write-Host "=== M-Social Login API @ $BaseUrl ===" -ForegroundColor Cyan
Write-Host "User: $UserId" -ForegroundColor Cyan

$ok = $true
$ok = (Step "GET /api/health" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/health" -UseBasicParsing -TimeoutSec 15
    if ($r.StatusCode -ne 200) { throw "HTTP $($r.StatusCode)" }
    "body=$($r.Content)"
}) -and $ok

$ok = (Step "GET /api/users/accounts/id/has/{id}" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts/id/has/$UserId" -UseBasicParsing -TimeoutSec 15
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

$ok = (Step "GET /api/mface-images?account= (CSV 동기화)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/mface-images?&account=$seq" -WebSession $userSession -UseBasicParsing
    "HTTP $($r.StatusCode) body=$($r.Content)"
}) -and $ok

$ok = (Step "POST mface-images CSV upload" {
    $csvPath = Join-Path $env:TEMP "${UserId}_SocialData.csv"
    Set-Content $csvPath "Day_Count,Last_Login,Progress`n1,$(Get-Date -Format 'yyyy-MM-dd'),FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF`nDate,Time,Log" -Encoding UTF8
    $hostName = ([Uri]$BaseUrl).Host
    $jar = Join-Path $env:TEMP ("msocial-" + [Guid]::NewGuid().ToString('N') + ".txt")
    "# Netscape HTTP Cookie File" | Set-Content $jar -Encoding ascii
    $userSession.Cookies.GetCookies([Uri]$BaseUrl) | ForEach-Object {
        "$hostName`tFALSE`t/`tFALSE`t0`t$($_.Name)`t$($_.Value)" | Add-Content $jar -Encoding ascii
    }
    $status = & curl.exe -sS -o NUL -w "%{http_code}" -X POST ($BaseUrl + "/api/mface-images") `
        -F "user=$seq" -F "file=@$csvPath;filename=${UserId}_SocialData.csv;type=application/octet-stream" -b $jar -c $jar
    if ([int]$status -lt 200 -or [int]$status -ge 300) { throw "HTTP $status" }
    "HTTP $status"
    Remove-Item $csvPath, $jar -Force -ErrorAction SilentlyContinue
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
