param(
    [string]$BaseUrl = "http://127.0.0.1:21250"
)

$ErrorActionPreference = "Stop"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$verifyId = "mface" + (Get-Date -Format 'HHmmss')

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Body = $null)
    $uri = $BaseUrl.TrimEnd('/') + $Path
    if ($Method -eq "GET") {
        return Invoke-WebRequest -Uri $uri -Method Get -WebSession $session -UseBasicParsing
    }
    return Invoke-WebRequest -Uri $uri -Method $Method -WebSession $session -UseBasicParsing `
        -ContentType "application/json" -Body $Body
}

Write-Host "=== M-Face API Verification ==="
Write-Host "Base: $BaseUrl"

$r = Invoke-Api GET "/api/health"
Write-Host "[PASS] health $($r.Content)"

$adminLogin = '{"id":"super-admin","password":"super-admin!@#$"}'
Invoke-Api POST "/api/users/sign-in" $adminLogin | Out-Null
Write-Host "[PASS] sign-in alias"

$createJson = (@{
    level = 1; domain = "spike"; id = $verifyId; password = "testpw1"
    name = "mfaceuser"; email = "$verifyId@gmail.com"; phone = "000"
} | ConvertTo-Json -Compress)
Invoke-Api POST "/api/users/accounts" $createJson | Out-Null
Write-Host "[PASS] create account (level alias)"

Invoke-Api GET "/api/users/sign-out" | Out-Null
$userLogin = (@{ id = $verifyId; password = "testpw1" } | ConvertTo-Json -Compress)
$loginResp = Invoke-Api POST "/api/users/sign-in" $userLogin
$seq = [regex]::Match($loginResp.Content, '"seq"\s*:\s*(\d+)').Groups[1].Value
Write-Host "[PASS] user sign-in seq=$seq"

$csvPath = Join-Path $env:TEMP "$verifyId`_FaceData.csv"
Set-Content $csvPath "Day_Count,Last_Login,Item`n1,2026-06-10,FFFFFFFFFFFFFFFF`nDate,Time,Log" -Encoding UTF8
$jpgPath = Join-Path $env:TEMP "mface-test.jpg"
[IO.File]::WriteAllBytes($jpgPath, [byte[]](0xFF,0xD8,0xFF,0xD9))

$jar = Join-Path $env:TEMP ("mface-" + [Guid]::NewGuid().ToString('N') + ".txt")
"# Netscape HTTP Cookie File" | Set-Content $jar
$session.Cookies.GetCookies([Uri]$BaseUrl) | ForEach-Object {
    "127.0.0.1`tFALSE`t/`tFALSE`t0`t$($_.Name)`t$($_.Value)" | Add-Content $jar
}

$csvOut = & curl.exe -sS -X POST ($BaseUrl + "/api/mface-images") `
    -F "user=$seq" -F "file=@$csvPath;type=application/octet-stream" -b $jar -c $jar
Write-Host "[PASS] csv upload $csvOut"

$list = Invoke-RestMethod -Uri ($BaseUrl + "/api/mface-images?account=$seq") -WebSession $session
if ($list.name -notcontains "$verifyId`_FaceData.csv") { throw "CSV not listed" }
Write-Host "[PASS] list images count=$($list.Count)"

$jpgOut = & curl.exe -sS -X POST ($BaseUrl + "/api/mface-images") `
    -F "user=$seq" -F "file=@$jpgPath;type=image/jpeg;filename=${verifyId}_Stage1_test_Happy.jpg" -b $jar -c $jar
if ($jpgOut -notmatch "emotion") { throw "emotion missing: $jpgOut" }
Write-Host "[PASS] jpg upload with emotion stub"

Invoke-Api GET "/api/users/sign-out" | Out-Null
Write-Host "[PASS] sign-out alias"
Write-Host "SUMMARY: M-Face API checks passed"
