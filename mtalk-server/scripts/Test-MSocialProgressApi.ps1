# M-Social progress API verification (SocialData CSV server sync)
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080/msocial"
)

$ErrorActionPreference = "Continue"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$testId = "ms_prog_" + (Get-Date -Format "yyyyMMddHHmmss")
$testPw = "pw1234"

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

Write-Host "=== M-Social Progress API @ $BaseUrl ===" -ForegroundColor Cyan

$ok = $true
$ok = (Step "POST /api/users/sign-in (super-admin)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $adminSession `
        -ContentType "application/json" -Body '{"id":"super-admin","password":"super-admin!@#$"}' -UseBasicParsing
    "HTTP $($r.StatusCode)"
}) -and $ok

$ok = (Step "POST /api/users/accounts (test user)" {
    $body = @{ level=1; domain="spike"; id=$testId; password=$testPw; name="MSocial Test"; email="test@hudit.local"; phone="010" } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/accounts" -Method POST -WebSession $adminSession `
        -ContentType "application/json" -Body $body -UseBasicParsing
    "HTTP $($r.StatusCode)"
}) -and $ok

$ok = (Step "POST /api/users/sign-in (test user)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $session `
        -ContentType "application/json" -Body (@{ id=$testId; password=$testPw } | ConvertTo-Json -Compress) -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.seq -le 0) { throw "seq missing" }
    "seq=$($obj.seq)"
}) -and $ok

$ok = (Step "GET /api/players/me/progress" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -WebSession $session -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.dayCount -lt 1) { throw "dayCount invalid" }
    if ($obj.progressString.Length -ne 32) { throw "progressString invalid: $($obj.progressString)" }
    "dayCount=$($obj.dayCount) progressString=$($obj.progressString)"
}) -and $ok

$ok = (Step "PUT /api/players/me/progress" {
    $body = @{ dayCount=2; progressString="TTTTTTTTFFFFFFFFFFFFFFFFFFFFFFFF" } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -Method PUT -WebSession $session `
        -ContentType "application/json" -Body $body -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.progressString -ne "TTTTTTTTFFFFFFFFFFFFFFFFFFFFFFFF") { throw "update failed: $($r.Content)" }
    "updated progressString=$($obj.progressString)"
}) -and $ok

$ok = (Step "POST /api/players/me/activity-logs" {
    $body = @{ logDate="2026-07-05"; logTime="07.30.00"; logBody="{Stage:FeelGood;Progress:(1/4);Scene:1;SelectedEmotion:Happy;isCorrect:True}" } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/activity-logs" -Method POST -WebSession $session `
        -ContentType "application/json" -Body $body -UseBasicParsing
    "HTTP $($r.StatusCode)"
}) -and $ok

$ok = (Step "GET /api/players/me/activity-logs" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/activity-logs" -WebSession $session -UseBasicParsing
    $arr = $r.Content | ConvertFrom-Json
    if (-not $arr -or $arr.Count -lt 1) { throw "no logs" }
    "count=$($arr.Count)"
}) -and $ok

if ($ok) {
    Write-Host "`nRESULT: ALL PASS" -ForegroundColor Green
    exit 0
}
Write-Host "`nRESULT: SOME FAILED" -ForegroundColor Red
exit 1
