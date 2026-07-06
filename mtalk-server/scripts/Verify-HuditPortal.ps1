param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080"
)

$ErrorActionPreference = "Stop"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$passed = 0
$failed = 0
$results = @()

function Test-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )
    try {
        & $Action
        $script:passed++
        $script:results += [pscustomobject]@{ Step = $Name; Result = "PASS" }
        Write-Host "[PASS] $Name" -ForegroundColor Green
    } catch {
        $script:failed++
        $script:results += [pscustomobject]@{ Step = $Name; Result = "FAIL"; Detail = $_.Exception.Message }
        Write-Host "[FAIL] $Name - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "HUDIT Portal verification @ $BaseUrl" -ForegroundColor Cyan

Test-Step "GET /health" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/health" -UseBasicParsing
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
}

Test-Step "GET / (login page)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/" -UseBasicParsing -WebSession $session
    if ($r.Content -notmatch "HUDIT") { throw "login page missing HUDIT marker" }
}

Test-Step "GET /hudit/dashboard.html assets" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/hudit/css/portal.css" -UseBasicParsing
    if ($r.StatusCode -ne 200) { throw "css missing" }
}

Test-Step "POST /api/hudit/auth/login (hudit)" {
    $body = '{"loginId":"hudit","password":"hudit!"}'
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/auth/login" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if (-not $json.authenticated) { throw "not authenticated" }
}

Test-Step "GET /api/hudit/auth/session" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/auth/session" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if (-not $json.authenticated) { throw "session not authenticated" }
}

Test-Step "GET /api/hudit/mtalk/services" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/services" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($json.Count -ne 4) { throw "expected 4 services, got $($json.Count)" }
}

$testId = "hudit-test-" + (Get-Date -Format "yyyyMMddHHmmss")
$testPw = "TestPass123!"

foreach ($service in @("mbench", "mbody", "mface", "msocial")) {
    Test-Step "GET /api/hudit/mtalk/$service/users" {
        $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/$service/users" -UseBasicParsing -WebSession $session
        if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
    }
}

Test-Step "POST create user on mbench" {
    $body = @{ id = $testId; password = $testPw; name = "HUDIT Test"; email = "test@hudit.local"; phone = "010" } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/mbench/users" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    $script:testSeq = $json.seq
    if (-not $script:testSeq) { throw "seq missing" }
}

Test-Step "PUT update user on mbench" {
    $newPw = "UpdatedPass456!"
    $body = @{ password = $newPw } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/mbench/users/$($script:testSeq)" -Method PUT -Body $body -ContentType "application/json" -UseBasicParsing -WebSession $session
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
}

Test-Step "POST reset password on mbench (random)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/mbench/users/$($script:testSeq)/reset-password" -Method POST -Body "{}" -ContentType "application/json" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if (-not $json.tempPassword) { throw "tempPassword missing" }
    if ($json.tempPassword.Length -lt 12) { throw "random password too short" }
}

Test-Step "POST reset password on mbench (custom)" {
    $customPw = "AdminSet789!"
    $body = @{ password = $customPw } | ConvertTo-Json -Compress
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/mtalk/mbench/users/$($script:testSeq)/reset-password" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($json.tempPassword -ne $customPw) { throw "custom password not applied" }
}

Test-Step "GET /hudit/cnt page" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/hudit/cnt" -UseBasicParsing -WebSession $session
    if ($r.Content -notmatch "CNT") { throw "cnt page missing marker" }
}

Test-Step "GET /api/hudit/cnt/config" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/config" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if (-not $json.legacyBaseUrl) { throw "legacyBaseUrl missing" }
}

Test-Step "GET /api/hudit/cnt/centers (sort/paging)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/centers?sort=centerId&order=asc&page=1&size=10" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($null -eq $json.total) { throw "total missing" }
    if ($json.size -ne 10) { throw "size not applied" }
}

Test-Step "GET /api/hudit/cnt/members (sort/paging)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members?sort=userName&order=asc&page=1&size=20" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($null -eq $json.items) { throw "items missing" }
}

Test-Step "GET /api/hudit/cnt/members (sort/paging)" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members?sort=centerNm&order=asc&page=1&size=20" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($null -eq $json.items) { throw "items missing" }
    if ($json.items.Count -gt 0 -and -not $json.items[0].PSObject.Properties['centerNm']) { throw "centerNm missing" }
}

Test-Step "GET /api/hudit/cnt/centers/{no}" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/centers?sort=no&order=asc&page=1&size=1" -UseBasicParsing -WebSession $session
    $page = $r.Content | ConvertFrom-Json
    if (-not $page.items -or $page.items.Count -lt 1) { throw "no center to test" }
    $centerNo = $page.items[0].no
    $r2 = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/centers/$centerNo" -UseBasicParsing -WebSession $session
    $center = $r2.Content | ConvertFrom-Json
    if (-not $center.centerId) { throw "centerId missing" }
}

Test-Step "GET /api/hudit/cnt/members by centerId" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/centers?sort=no&order=asc&page=1&size=1" -UseBasicParsing -WebSession $session
    $page = $r.Content | ConvertFrom-Json
    $centerId = $page.items[0].centerId
    $r2 = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members?centerId=$centerId&page=1&size=10" -UseBasicParsing -WebSession $session
    if ($r2.StatusCode -ne 200) { throw "status $($r2.StatusCode)" }
}

Test-Step "GET /api/hudit/cnt/members/{userId}/analysis" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members?sort=no&order=asc&page=1&size=1" -UseBasicParsing -WebSession $session
    $page = $r.Content | ConvertFrom-Json
    if (-not $page.items -or $page.items.Count -lt 1) { throw "no member to test" }
    $userId = $page.items[0].userId
    $encoded = [uri]::EscapeDataString($userId)
    $r2 = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members/$encoded/analysis" -UseBasicParsing -WebSession $session
    $json = $r2.Content | ConvertFrom-Json
    if (-not $json.member.userId) { throw "member missing" }
    if ($json.sections.Count -lt 5) { throw "expected 5 analysis sections" }
    $script:testUserId = $userId
}

Test-Step "GET /hudit/cnt page assets" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/hudit/cnt" -UseBasicParsing -WebSession $session
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
    if ($r.Content -notmatch "cnt-bulk-pdf.js") { throw "bulk pdf script missing" }
    if ($r.Content -notmatch "center-bulk-pdf") { throw "bulk pdf action missing" }
    if ($r.Content -notmatch "bulk-pdf-stats") { throw "bulk pdf stats missing" }
    $js = Invoke-WebRequest -Uri "$BaseUrl/hudit/js/cnt-bulk-pdf.js" -UseBasicParsing -WebSession $session
    if ($js.StatusCode -ne 200) { throw "cnt-bulk-pdf.js status $($js.StatusCode)" }
    if ($js.Content -notmatch "CntBulkPdf") { throw "CntBulkPdf module missing" }
}

Test-Step "GET /api/hudit/cnt/members by centerId for bulk" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members?centerId=KMB&page=1&size=5" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($json.total -lt 1) { throw "expected KMB members" }
    if (-not $json.items[0].userId) { throw "member userId missing" }
}

Test-Step "GET /api/hudit/cnt/members/{userId}/pdf proxy" {
    $encoded = [uri]::EscapeDataString($script:testUserId)
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/members/$encoded/pdf" -UseBasicParsing -WebSession $session
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
    if ($r.Content -notmatch "html2pdf") { throw "html2pdf script missing" }
    if ($r.Content -notmatch "CNT REPORT") { throw "CNT PDF report marker missing" }
    if ($r.Content -notmatch "/api/hudit/cnt/pdf-assets/css/commonpdf.css") { throw "proxied css path missing" }
    if ($r.Content -match 'href="http://cnt\.re\.kr/') { throw "legacy http asset url leaked into html" }
    if ($r.Content -notmatch "downloadBtn") { throw "download button missing" }
}

Test-Step "GET /api/hudit/cnt/pdf-assets/css/commonpdf.css" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/cnt/pdf-assets/css/commonpdf.css" -UseBasicParsing -WebSession $session
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
    if ($r.Content -notmatch "CNT") { throw "commonpdf.css content missing" }
    if ($r.Content -match 'url\(http://cnt\.re\.kr') { throw "legacy http url leaked into css" }
}

Test-Step "POST /api/hudit/auth/logout" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/auth/logout" -Method POST -UseBasicParsing -WebSession $session
    if ($r.StatusCode -ne 200) { throw "status $($r.StatusCode)" }
}

Test-Step "GET /api/hudit/auth/session after logout" {
    $r = Invoke-WebRequest -Uri "$BaseUrl/api/hudit/auth/session" -UseBasicParsing -WebSession $session
    $json = $r.Content | ConvertFrom-Json
    if ($json.authenticated) { throw "still authenticated" }
}

Write-Host ""
Write-Host "Summary: $passed passed, $failed failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
$results | Format-Table -AutoSize

if ($failed -gt 0) { exit 1 }
