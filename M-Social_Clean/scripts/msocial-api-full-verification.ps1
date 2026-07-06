# M-Social API verification against MBody_Revised_server (shared server)
# M-Social uses sign-in/sign-out aliases and mface-images for data sync
param(
    [string]$BaseUrl = "http://127.0.0.1:21250"
)

$ErrorActionPreference = "Continue"
$results = New-Object System.Collections.Generic.List[object]
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$reportDir = Join-Path (Split-Path $PSScriptRoot -Parent) "docs"
$reportPath = Join-Path $reportDir "MSOCIAL_LOCAL_API_VERIFICATION_LATEST.md"

function Add-Result($category, $name, $pass, $detail, $unityRef) {
    $results.Add([PSCustomObject]@{
        Category = $category; Name = $name; Pass = $pass; Detail = $detail; UnityRef = $unityRef
    }) | Out-Null
    $mark = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] [$category] $name - $detail"
}

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Body = $null, $Session = $session)
    $uri = $BaseUrl.TrimEnd('/') + $Path
    if ($Method -eq "GET") {
        return Invoke-WebRequest -Uri $uri -Method Get -WebSession $Session -UseBasicParsing
    }
    return Invoke-WebRequest -Uri $uri -Method $Method -WebSession $Session -UseBasicParsing `
        -ContentType "application/json" -Body $Body
}

function New-Session { return New-Object Microsoft.PowerShell.Commands.WebRequestSession }

function Get-UnitySeq([string]$text) {
    try {
        $obj = $text | ConvertFrom-Json
        if ($null -ne $obj.seq -and [int]$obj.seq -gt 0) { return [string]$obj.seq }
    } catch {}
    throw "seq parse failed: $text"
}

Write-Host "=== M-Social API Verification (MBody Shared Server) ==="
Write-Host "Base: $BaseUrl"

$accountId = "ms_" + (Get-Date -Format "HHmmss")
$userSeq = $null

# A. Health
try {
    $r = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/health" -UseBasicParsing
    Add-Result "A-connect" "server health" ($r.StatusCode -eq 200) "HTTP $($r.StatusCode) $($r.Content)" "Login.cs url"
} catch { Add-Result "A-connect" "server health" $false $_.Exception.Message "" }

# B. Account check
try {
    $r = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/users/accounts/id/has/$accountId" -UseBasicParsing
    Add-Result "B-auth" "new account has=false" ($r.Content -eq "false") $r.Content "Login.AdminLogin L97"
} catch { Add-Result "B-auth" "new account has=false" $false $_.Exception.Message "" }

# C. Bootstrap with sign-in alias (M-Social uses sign-in, not login)
$adminSession = New-Session
try {
    $body = '{"id":"super-admin","password":"super-admin!@#$"}'
    $r = Invoke-Api POST "/api/users/sign-in" $body $adminSession
    Add-Result "C-bootstrap" "super-admin sign-in" ($r.StatusCode -eq 200) "JSESSIONID=$($adminSession.Cookies.GetCookies([Uri]$BaseUrl).Count -gt 0)" "Login L114"
} catch { Add-Result "C-bootstrap" "super-admin sign-in" $false $_.Exception.Message "" }

try {
    $body = (@{
        level = 1; domain = "spike"; id = $accountId; password = "socialpw1"
        name = "socialuser"; email = "$accountId@gmail.com"; phone = "000"
    } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/accounts" $body $adminSession
    $userSeq = Get-UnitySeq $r.Content
    Add-Result "C-bootstrap" "create account (level field)" ($r.StatusCode -eq 200) $r.Content "Login L116"
} catch { Add-Result "C-bootstrap" "create account" $false $_.Exception.Message "" }

try {
    $r = Invoke-Api GET "/api/users/sign-out" $null $adminSession
    Add-Result "C-bootstrap" "bootstrap sign-out" ($r.StatusCode -eq 200) $r.Content "Login L131"
} catch { Add-Result "C-bootstrap" "bootstrap sign-out" $false $_.Exception.Message "" }

# D. User sign-in
try {
    $body = (@{ id = $accountId; password = "socialpw1" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/sign-in" $body $session
    $userSeq = Get-UnitySeq $r.Content
    Add-Result "D-auth" "user sign-in + seq" ($r.StatusCode -eq 200) "seq=$userSeq" "Login.sendPost L121"
} catch { Add-Result "D-auth" "user sign-in" $false $_.Exception.Message "" }

# E. Existing account
try {
    $r = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/users/accounts/id/has/$accountId" -UseBasicParsing
    Add-Result "E-auth" "existing account has=true" ($r.Content -eq "true") $r.Content "Login existing branch"
} catch { Add-Result "E-auth" "existing account has=true" $false $_.Exception.Message "" }

# F. mface-images list (M-Social data sync)
try {
    $r = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/mface-images?account=$userSeq" -UseBasicParsing
    $items = $r.Content | ConvertFrom-Json
    Add-Result "F-sync" "list mface-images by account" ($r.StatusCode -eq 200) "count=$($items.Count)" "Login.cs L188"
} catch { Add-Result "F-sync" "list mface-images" $false $_.Exception.Message "" }

# G. mface-images upload (CSV sync)
try {
    $csvContent = "ItemString,Stage,Emotion`nTTTTTTTTTTTTTTTT,1,0"
    $tmp = Join-Path $env:TEMP ("msocial_" + [Guid]::NewGuid().ToString('N') + ".csv")
    Set-Content $tmp -Value $csvContent -Encoding UTF8
    $jar = Join-Path $env:TEMP ("msocial-cookie-" + [Guid]::NewGuid().ToString('N') + ".txt")
    $cookieLines = @("# Netscape HTTP Cookie File")
    $session.Cookies.GetCookies([Uri]$BaseUrl) | ForEach-Object {
        $cookieLines += ("127.0.0.1`tFALSE`t/`tFALSE`t0`t" + $_.Name + "`t" + $_.Value)
    }
    Set-Content $jar -Value $cookieLines -Encoding ascii
    $out = & curl.exe -sS -X POST ($BaseUrl.TrimEnd('/') + "/api/mface-images") -F "user=$userSeq" -F "file=@$tmp;type=application/octet-stream" -b $jar -c $jar -w "`nHTTP_STATUS:%{http_code}"
    $status = [int](($out | Select-String 'HTTP_STATUS:(\d+)').Matches[0].Groups[1].Value)
    $body = ($out -replace "`nHTTP_STATUS:\d+", "").Trim()
    $ok = ($status -eq 200 -and $body -match '"key"')
    Add-Result "G-upload" "CSV upload via mface-images" $ok $body "Login.cs Upload"
    Remove-Item $tmp, $jar -Force -ErrorAction SilentlyContinue
} catch { Add-Result "G-upload" "CSV upload" $false $_.Exception.Message "" }

# H. Negative tests
try {
    $badBody = (@{ id = $accountId; password = "wrongpass" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/sign-in" $badBody (New-Session)
    Add-Result "H-negative" "wrong password" $false "expected 401 got $($r.StatusCode)" ""
} catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    Add-Result "H-negative" "wrong password" ($code -eq 401) "HTTP $code" ""
}

try {
    $body = (@{ level = 1; domain = "spike"; id = "ms_hack"; password = "x"; name = "x"; email = "x@x.com"; phone = "0" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/accounts" $body (New-Session)
    Add-Result "H-negative" "create without admin" $false "expected 403 got $($r.StatusCode)" ""
} catch {
    $code = 0
    if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
    Add-Result "H-negative" "create without admin" ($code -eq 403) "HTTP $code" ""
}

# I. Sign-out
try {
    $r = Invoke-Api GET "/api/users/sign-out" $null $session
    Add-Result "I-auth" "user sign-out" ($r.StatusCode -eq 200) $r.Content "Login.LogOut"
} catch { Add-Result "I-auth" "user sign-out" $false $_.Exception.Message "" }

# J. Data persist
if ($userSeq) {
    try {
        $listR = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/mface-images?account=$userSeq" -UseBasicParsing
        $items = $listR.Content | ConvertFrom-Json
        $hasR = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/users/accounts/id/has/$accountId" -UseBasicParsing
        $ok = ($hasR.Content -eq "true" -and $items.Count -ge 1)
        Add-Result "J-db" "data persist" $ok "has=true images=$($items.Count)" ""
    } catch { Add-Result "J-db" "data persist" $false $_.Exception.Message "" }
}

$passed = @($results | Where-Object { $_.Pass }).Count
$failed = @($results | Where-Object { -not $_.Pass }).Count
$total = $results.Count
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$md = @()
$md += "# M-Social Local API Verification (MBody Shared Server)"
$md += ""
$md += "> Run: $ts"
$md += "> Server: MBody_Revised_server @ $BaseUrl"
$md += "> Result: **$passed / $total PASS**"
$md += ""
$md += "## Test Results"
$md += ""
$md += "| Category | Test | Pass | Detail | Unity Ref |"
$md += "|----------|------|------|--------|-----------|"
foreach ($r in $results) {
    $m = if ($r.Pass) { "OK" } else { "NG" }
    $md += "| $($r.Category) | $($r.Name) | $m | $($r.Detail) | $($r.UnityRef) |"
}
$md += ""
if ($failed -eq 0) { $md += "All M-Social flows verified against MBody shared server." }
else { $md += "$failed test(s) failed." }

Set-Content -Path $reportPath -Value ($md -join "`n") -Encoding UTF8
Write-Host ""
Write-Host "SUMMARY: $passed / $total passed"
Write-Host "Report: $reportPath"
if ($failed -gt 0) { exit 1 }
exit 0
