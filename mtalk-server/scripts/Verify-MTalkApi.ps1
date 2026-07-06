# MTalk 통합 서버 API 검증 (4개 서비스 경로)
param(
    [string]$BaseUrl = "http://127.0.0.1:8080"
)

$ErrorActionPreference = "Continue"
$pass = 0; $fail = 0

function Step($name, [scriptblock]$action) {
    try {
        $detail = & $action
        Write-Host "[PASS] $name - $detail" -ForegroundColor Green
        $script:pass++
    } catch {
        Write-Host "[FAIL] $name - $($_.Exception.Message)" -ForegroundColor Red
        $script:fail++
    }
}

Write-Host "MTalk unified API verification: $BaseUrl`n" -ForegroundColor Cyan

Step "GET / (root)" {
    $r = Invoke-RestMethod "$BaseUrl/"
    if ($r.status -ne "UP") { throw "status=$($r.status)" }
    "routes=$($r.routes.Keys -join ',')"
}

Step "GET /mbench/health" {
    $r = Invoke-RestMethod "$BaseUrl/mbench/health"
    if ($r.status -ne "UP") { throw $r.status }
    "service=$($r.service)"
}

Step "GET /mbody/api/health" {
    $r = Invoke-RestMethod "$BaseUrl/mbody/api/health"
    if ($r.status -ne "ok") { throw $r.status }
    "ok"
}

Step "GET /mface/api/health" {
    $r = Invoke-RestMethod "$BaseUrl/mface/api/health"
    if ($r.status -ne "ok") { throw $r.status }
    "service=$($r.service)"
}

Step "GET /msocial/api/health" {
    $r = Invoke-RestMethod "$BaseUrl/msocial/api/health"
    if ($r.status -ne "ok") { throw $r.status }
    "service=$($r.service)"
}

# M-BENCH flow
$mbId = "mt_mbench_" + (Get-Date -Format "HHmmss")
$mbSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Step "M-BENCH admin login" {
    $body = '{"id":"super-admin","password":"super-admin!@#$"}'
    $r = Invoke-WebRequest "$BaseUrl/mbench/api/users/login" -Method POST -Body $body -ContentType "application/json" -WebSession $mbSession -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    if (-not $j.success) { throw $r.Content }
    "seq=$($j.seq)"
}
Step "M-BENCH create account" {
    $body = (@{ levelId=1; domain="spike"; id=$mbId; password="pw1234"; name="t"; email="t@t.com"; phone="0" } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest "$BaseUrl/mbench/api/users/accounts" -Method POST -Body $body -ContentType "application/json" -WebSession $mbSession -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    if (-not $j.success) { throw $r.Content }
    "id=$mbId"
}

# M-BODY flow
$mdId = "s_mt_" + (Get-Date -Format "HHmmss")
$mdSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Step "M-BODY bootstrap + login" {
    $adm = '{"id":"super-admin","password":"super-admin!@#$"}'
    Invoke-WebRequest "$BaseUrl/mbody/api/users/login" -Method POST -Body $adm -ContentType "application/json" -WebSession $mdSession -UseBasicParsing | Out-Null
    $body = (@{ levelId=10; domain="spike"; id=$mdId; password="pw1234"; name="u"; email="u@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-WebRequest "$BaseUrl/mbody/api/users/accounts" -Method POST -Body $body -ContentType "application/json" -WebSession $mdSession -UseBasicParsing | Out-Null
    Invoke-WebRequest "$BaseUrl/mbody/api/users/logout" -WebSession $mdSession -UseBasicParsing | Out-Null
    $login = (@{ id=$mdId; password="pw1234" } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest "$BaseUrl/mbody/api/users/login" -Method POST -Body $login -ContentType "application/json" -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    if (-not $j.seq) { throw $r.Content }
    "seq=$($j.seq)"
}

# M-FACE flow
$mfId = "mf_mt_" + (Get-Date -Format "HHmmss")
$mfSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Step "M-FACE sign-in" {
    $adm = '{"id":"super-admin","password":"super-admin!@#$"}'
    Invoke-WebRequest "$BaseUrl/mface/api/users/sign-in" -Method POST -Body $adm -ContentType "application/json" -WebSession $mfSession -UseBasicParsing | Out-Null
    $body = (@{ level=1; domain="spike"; id=$mfId; password="pw1234"; name="f"; email="f@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-WebRequest "$BaseUrl/mface/api/users/accounts" -Method POST -Body $body -ContentType "application/json" -WebSession $mfSession -UseBasicParsing | Out-Null
    Invoke-WebRequest "$BaseUrl/mface/api/users/sign-out" -WebSession $mfSession -UseBasicParsing | Out-Null
    $login = (@{ id=$mfId; password="pw1234" } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest "$BaseUrl/mface/api/users/sign-in" -Method POST -Body $login -ContentType "application/json" -WebSession $mfSession -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    if (-not $j.seq) { throw $r.Content }
    "seq=$($j.seq) user=$mfId"
}

# M-SOCIAL flow
$msId = "ms_mt_" + (Get-Date -Format "HHmmss")
$msSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
Step "M-SOCIAL sign-in" {
    $adm = '{"id":"super-admin","password":"super-admin!@#$"}'
    Invoke-WebRequest "$BaseUrl/msocial/api/users/sign-in" -Method POST -Body $adm -ContentType "application/json" -WebSession $msSession -UseBasicParsing | Out-Null
    $body = (@{ level=1; domain="spike"; id=$msId; password="pw1234"; name="s"; email="s@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-WebRequest "$BaseUrl/msocial/api/users/accounts" -Method POST -Body $body -ContentType "application/json" -WebSession $msSession -UseBasicParsing | Out-Null
    Invoke-WebRequest "$BaseUrl/msocial/api/users/sign-out" -WebSession $msSession -UseBasicParsing | Out-Null
    $login = (@{ id=$msId; password="pw1234" } | ConvertTo-Json -Compress)
    $r = Invoke-WebRequest "$BaseUrl/msocial/api/users/sign-in" -Method POST -Body $login -ContentType "application/json" -WebSession $msSession -UseBasicParsing
    $j = $r.Content | ConvertFrom-Json
    if (-not $j.seq) { throw $r.Content }
    "seq=$($j.seq) user=$msId"
}

Write-Host "`nResult: $pass passed, $fail failed" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
if ($fail -gt 0) { exit 1 }
