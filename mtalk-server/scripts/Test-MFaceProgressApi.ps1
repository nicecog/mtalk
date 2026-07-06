# M-Face progress API verification (FaceData CSV server sync)
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

Write-Host "=== M-Face Progress API @ $BaseUrl ===" -ForegroundColor Cyan

$ok = $true
$ok = (Step "POST /api/users/sign-in" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/users/sign-in" -Method POST -WebSession $session `
        -ContentType "application/json" -Body (@{ id=$UserId; password=$Password } | ConvertTo-Json -Compress) -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.seq -le 0) { throw "seq missing" }
    "seq=$($obj.seq)"
}) -and $ok

$ok = (Step "GET /api/players/me/progress" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -WebSession $session -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.dayCount -lt 1) { throw "dayCount invalid" }
    if ($obj.itemString.Length -ne 16) { throw "itemString invalid: $($obj.itemString)" }
    "dayCount=$($obj.dayCount) itemString=$($obj.itemString)"
}) -and $ok

$ok = (Step "PUT /api/players/me/progress" {
    $body = @{ dayCount=2; itemString="TTTTFFFFFFFFFFFF" } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/players/me/progress" -Method PUT -WebSession $session `
        -ContentType "application/json" -Body $body -UseBasicParsing
    $obj = $r.Content | ConvertFrom-Json
    if ($obj.itemString -ne "TTTTFFFFFFFFFFFF") { throw "update failed: $($r.Content)" }
    "updated itemString=$($obj.itemString)"
}) -and $ok

$ok = (Step "POST /api/players/me/activity-logs" {
    $body = @{ logDate="2026-07-05"; logTime="07.30.00"; logBody="{Stage:1;Point:(3/5);Face:12}" } | ConvertTo-Json -Compress
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
