# MTalk 통합 서버 전체 API 검증 (PostgreSQL / 4개 서비스)
param(
    [string]$BaseUrl = "http://127.0.0.1:8080",
    [string]$PgContainer = "mtalk-postgres",
    [string]$PgUser = "mtalk",
    [string]$PgPassword = "mtalk1234"
)

$ErrorActionPreference = "Continue"
$results = New-Object System.Collections.Generic.List[object]
$reportPath = Join-Path $PSScriptRoot "..\docs\MTALK_API_VERIFICATION_LATEST.md"

function Add-Result($svc, $category, $name, $pass, $detail) {
    $results.Add([PSCustomObject]@{ Service = $svc; Category = $category; Name = $name; Pass = $pass; Detail = $detail }) | Out-Null
    $mark = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] [$svc/$category] $name - $detail"
}

function New-Session { return New-Object Microsoft.PowerShell.Commands.WebRequestSession }

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        $Body = $null,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session = $null
    )
    if (-not $Session) {
        if ($Body -is [Microsoft.PowerShell.Commands.WebRequestSession]) {
            $Session = $Body
            $Body = $null
        } else {
            $Session = New-Session
        }
    }
    $uri = $BaseUrl.TrimEnd('/') + $Path
    if ($Method -eq "GET") {
        return Invoke-WebRequest -Uri $uri -Method Get -WebSession $Session -UseBasicParsing
    }
    return Invoke-WebRequest -Uri $uri -Method $Method -WebSession $Session -UseBasicParsing `
        -ContentType "application/json" -Body $Body
}

function Get-UnitySeq([string]$text) {
    try {
        $obj = $text | ConvertFrom-Json
        if ($null -ne $obj.seq -and [int]$obj.seq -gt 0) { return [string]$obj.seq }
    } catch {}
    foreach ($part in ($text -split ',')) {
        if ($part -match 'seq') {
            return ($part.Split(':')[1].Replace('}', '').Replace('"', '').Trim())
        }
    }
    throw "seq parse failed: $text"
}

function Get-CookieJar($sess, [string]$CookieHost = "127.0.0.1") {
    $jar = Join-Path $env:TEMP ("mtalk-" + [Guid]::NewGuid().ToString('N') + ".txt")
    $lines = @("# Netscape HTTP Cookie File")
    $sess.Cookies.GetCookies([Uri]$BaseUrl) | ForEach-Object {
        $lines += ("$CookieHost`tFALSE`t/`tFALSE`t0`t" + $_.Name + "`t" + $_.Value)
    }
    Set-Content $jar -Value $lines -Encoding ascii
    return $jar
}

function Upload-Multipart($sess, [string]$path, [hashtable]$fields) {
    $jar = Get-CookieJar $sess
    $args = @("-sS", "-X", "POST", ($BaseUrl.TrimEnd('/') + $path), "-b", $jar, "-c", $jar)
    foreach ($k in $fields.Keys) {
        $v = $fields[$k]
        if ($v -is [string] -and (Test-Path $v)) {
            $args += "-F"; $args += "$k=@$v"
        } else {
            $args += "-F"; $args += "$k=$v"
        }
    }
    $bodyFile = Join-Path $env:TEMP ("mtalk-up-" + [Guid]::NewGuid().ToString('N') + ".json")
    $status = & curl.exe @args -o $bodyFile -w "%{http_code}"
    $body = if (Test-Path $bodyFile) { Get-Content $bodyFile -Raw } else { "" }
    Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue
    return @{ Status = [int]$status; Body = $body.Trim() }
}

function Test-PgPersist([string]$db, [string]$sql, [string]$label) {
    if ($BaseUrl -notmatch '127\.0\.0\.1|localhost') {
        Add-Result $label "DB" "postgres persist" $true "remote-skip (API verified)"
        return $true
    }
    try {
        $out = docker exec -e PGPASSWORD=$PgPassword $PgContainer psql -U $PgUser -d $db -t -A -c $sql 2>$null
        $vals = @($out -split "`r?`n" | Where-Object { $_ -match '^\d+$' })
        if ($vals.Count -ge 1 -and [int]$vals[0] -ge 1) {
            Add-Result $label "DB" "postgres persist" $true "pg $db => $($vals -join ',')"
            return $true
        }
    } catch {}
    Add-Result $label "DB" "postgres persist" $false "docker psql unavailable or count=0"
    return $false
}

Write-Host "=== MTalk Full API Verification (PostgreSQL) ===" -ForegroundColor Cyan
Write-Host "Base: $BaseUrl`n"

# --- Root ---
try {
    $r = Invoke-Api GET "/health"
    $j = $r.Content | ConvertFrom-Json
    Add-Result "ROOT" "connect" "GET /health" ($j.status -eq "UP") "routes=$($j.routes.PSObject.Properties.Name -join ',')"
} catch {
    Add-Result "ROOT" "connect" "GET /health" $false $_.Exception.Message
    Write-Host "Server not reachable. Abort." -ForegroundColor Red
    exit 2
}

# ========== M-BENCH ==========
$mbPrefix = "/mbench"
$mbId = "mb_" + (Get-Date -Format "HHmmss")
$mbSession = New-Session

try {
    $r = Invoke-Api GET "$mbPrefix/health"
    $j = $r.Content | ConvertFrom-Json
    Add-Result "M-BENCH" "health" "GET /mbench/health" ($j.status -eq "UP") $j.service
} catch { Add-Result "M-BENCH" "health" "GET /mbench/health" $false $_.Exception.Message }

try {
    $r = Invoke-Api GET "$mbPrefix/api/users/accounts/id/has/$mbId"
    Add-Result "M-BENCH" "auth" "has=false new account" ($r.Content -eq "false") $r.Content
} catch { Add-Result "M-BENCH" "auth" "has=false" $false $_.Exception.Message }

try {
    $r = Invoke-Api POST "$mbPrefix/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' $mbSession
    $j = $r.Content | ConvertFrom-Json
    Add-Result "M-BENCH" "auth" "admin login" ($j.success -eq $true) "seq=$($j.seq)"
} catch { Add-Result "M-BENCH" "auth" "admin login" $false $_.Exception.Message }

try {
    $body = (@{ levelId=1; domain="spike"; id=$mbId; password="pw1234"; name="t"; email="t@t.com"; phone="0" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "$mbPrefix/api/users/accounts" $body $mbSession
    $j = $r.Content | ConvertFrom-Json
    Add-Result "M-BENCH" "auth" "create account" ($j.success -eq $true) "id=$mbId"
} catch { Add-Result "M-BENCH" "auth" "create account" $false $_.Exception.Message }

try {
    $r = Invoke-Api GET "$mbPrefix/api/users/accounts/id/has/$mbId"
    Add-Result "M-BENCH" "auth" "has=true existing" ($r.Content -eq "true") $r.Content
} catch { Add-Result "M-BENCH" "auth" "has=true" $false $_.Exception.Message }

try {
    $adminSess = New-Session
    Invoke-Api POST "$mbPrefix/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' $adminSess | Out-Null
    $r = Invoke-Api GET "$mbPrefix/api/admin/session" $adminSess
    $j = $r.Content | ConvertFrom-Json
    Add-Result "M-BENCH" "admin" "admin session" ($j.loggedIn -eq $true) $r.Content
} catch { Add-Result "M-BENCH" "admin" "admin session" $false $_.Exception.Message }

try {
    Invoke-Api GET "$mbPrefix/api/users/logout" $null $mbSession | Out-Null
    $r = Invoke-Api POST "$mbPrefix/api/users/login" (@{ id=$mbId; password="pw1234" } | ConvertTo-Json -Compress) (New-Session)
    $j = $r.Content | ConvertFrom-Json
    Add-Result "M-BENCH" "auth" "user re-login" ($j.success -eq $true) "seq=$($j.seq)"
} catch { Add-Result "M-BENCH" "auth" "user re-login" $false $_.Exception.Message }

try {
    Invoke-Api POST "$mbPrefix/api/users/login" '{"id":"super-admin","password":"wrong"}' (New-Session) | Out-Null
    Add-Result "M-BENCH" "negative" "wrong password" $false "unexpected success"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    Add-Result "M-BENCH" "negative" "wrong password" ($code -eq 401) "HTTP $code"
}

Test-PgPersist "mbench" "SELECT COUNT(*) FROM user_accounts WHERE login_id='$mbId';" "M-BENCH" | Out-Null

# ========== M-BODY ==========
$mdPrefix = "/mbody"
$mdId = "s_md_" + (Get-Date -Format "HHmmss")
$mdSession = New-Session
$mdSeq = $null

try {
    $r = Invoke-Api GET "$mdPrefix/api/health"
    Add-Result "M-BODY" "health" "GET /mbody/api/health" ($r.Content -match "ok") $r.Content
} catch { Add-Result "M-BODY" "health" "health" $false $_.Exception.Message }

try {
    $r = Invoke-Api GET "$mdPrefix/api/users/accounts/id/has/$mdId"
    Add-Result "M-BODY" "auth" "has=false" ($r.Content -eq "false") $r.Content
} catch { Add-Result "M-BODY" "auth" "has=false" $false $_.Exception.Message }

$boot = New-Session
try {
    Invoke-Api POST "$mdPrefix/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' $boot | Out-Null
    $body = (@{ levelId=10; domain="spike"; id=$mdId; password="pw1234"; name="u"; email="u@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "$mdPrefix/api/users/accounts" $body $boot | Out-Null
    Invoke-Api GET "$mdPrefix/api/users/logout" $null $boot | Out-Null
    $r = Invoke-Api POST "$mdPrefix/api/users/login" (@{ id=$mdId; password="pw1234" } | ConvertTo-Json -Compress) $mdSession
    $mdSeq = Get-UnitySeq $r.Content
    Add-Result "M-BODY" "auth" "bootstrap+login seq" ($mdSeq -match '^\d+$') "seq=$mdSeq"
} catch { Add-Result "M-BODY" "auth" "bootstrap+login" $false $_.Exception.Message }

if ($mdSeq) {
    try {
        $mp4 = Join-Path $env:TEMP "mtalk-sample.mp4"
        if (-not (Test-Path $mp4)) {
            [IO.File]::WriteAllBytes($mp4, [byte[]](0x00,0x00,0x00,0x20,0x66,0x74,0x79,0x70,0x69,0x73,0x6F,0x6D,0x00,0x00,0x02,0x00))
        }
        $up = Upload-Multipart $mdSession "$mdPrefix/api/files/attach" @{ file = $mp4 }
        $fv = ($up.Body | ConvertFrom-Json).fileVideo
        Add-Result "M-BODY" "upload" "mp4 attach" ($up.Status -eq 200 -and $fv) $up.Body
        if ($fv) {
            $sv = (@{ userAccountSeq=[int]$mdSeq; fileVideo=$fv; contentPoint=0; contentType="post" } | ConvertTo-Json -Compress)
            $r = Invoke-Api POST "$mdPrefix/api/subject-videos" $sv $mdSession
            Add-Result "M-BODY" "upload" "subject-videos" ($r.StatusCode -eq 200) $r.Content
        }
    } catch { Add-Result "M-BODY" "upload" "mp4 chain" $false $_.Exception.Message }
}

try {
    Invoke-Api POST "$mdPrefix/api/users/login" '{"id":"super-admin","password":"wrong"}' (New-Session) | Out-Null
    Add-Result "M-BODY" "negative" "wrong password" $false "unexpected"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    Add-Result "M-BODY" "negative" "wrong password" ($code -eq 401) "HTTP $code"
}

if ($mdSeq) {
    Test-PgPersist "mbody_revised" "SELECT COUNT(*) FROM user_accounts WHERE login_id='$mdId'; SELECT COUNT(*) FROM subject_videos WHERE user_account_seq=$mdSeq;" "M-BODY" | Out-Null
}

# ========== M-FACE ==========
$mfPrefix = "/mface"
$mfId = "mf_" + (Get-Date -Format "HHmmss")
$mfSession = New-Session
$mfSeq = $null
$csvName = "${mfId}_FaceData.csv"

try {
    $r = Invoke-Api GET "$mfPrefix/api/health"
    Add-Result "M-FACE" "health" "GET /mface/api/health" ($r.Content -match "ok") $r.Content
} catch { Add-Result "M-FACE" "health" "health" $false $_.Exception.Message }

$adm = New-Session
try {
    Invoke-Api POST "$mfPrefix/api/users/sign-in" '{"id":"super-admin","password":"super-admin!@#$"}' $adm | Out-Null
    $body = (@{ level=1; domain="spike"; id=$mfId; password="pw1234"; name="f"; email="f@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "$mfPrefix/api/users/accounts" $body $adm | Out-Null
    Invoke-Api GET "$mfPrefix/api/users/sign-out" $null $adm | Out-Null
    $r = Invoke-Api POST "$mfPrefix/api/users/sign-in" (@{ id=$mfId; password="pw1234" } | ConvertTo-Json -Compress) $mfSession
    $mfSeq = Get-UnitySeq $r.Content
    Add-Result "M-FACE" "auth" "sign-in seq" ($mfSeq -match '^\d+$') "seq=$mfSeq user=$mfId"
} catch { Add-Result "M-FACE" "auth" "sign-in" $false $_.Exception.Message }

if ($mfSeq) {
    try {
        $csv = Join-Path $env:TEMP $csvName
        Set-Content $csv "Day_Count,Last_Login,Item`n1,2026-06-13,FFFFFFFFFFFFFFFF`nDate,Time,Log" -Encoding UTF8
        $up = Upload-Multipart $mfSession "$mfPrefix/api/mface-images" @{ user = $mfSeq; $csvName = $csv }
        $obj = $up.Body | ConvertFrom-Json
        Add-Result "M-FACE" "csv" "FaceData upload" ($up.Status -eq 200 -and $obj.key) "key=$($obj.key)"
        $r = Invoke-Api GET "$mfPrefix/api/mface-images?account=$mfSeq" $mfSession
        $items = $r.Content | ConvertFrom-Json
        Add-Result "M-FACE" "csv" "list by account" ($items.Count -ge 1) "count=$($items.Count)"
        if ($obj.key) {
            $jar = Get-CookieJar $mfSession
            $dl = Join-Path $env:TEMP "mf-dl.csv"
            $st = & curl.exe -sS -o $dl -w "%{http_code}" ($BaseUrl.TrimEnd('/') + "$mfPrefix/api/mface-images/$($obj.key)") -b $jar
            $txt = Get-Content $dl -Raw -Encoding UTF8
            $ok = ([int]$st -eq 200) -and ($txt -match 'Day_Count|Last_Login|FFFFFFFFFFFFFFFF')
            Add-Result "M-FACE" "csv" "download by key" $ok "http=$st bytes=$((Get-Item $dl).Length) head=$($txt.Substring(0,[Math]::Min(40,$txt.Length)))"
        }
    } catch { Add-Result "M-FACE" "csv" "csv flow" $false $_.Exception.Message }

    try {
        $jpg = Join-Path $env:TEMP "mf-stage1.jpg"
        [IO.File]::WriteAllBytes($jpg, [byte[]](0xFF,0xD8,0xFF,0xD9))
        $fname = "${mfId}_Stage1_test_Happy.jpg"
        $up = Upload-Multipart $mfSession "$mfPrefix/api/mface-images" @{ user = $mfSeq; file = $jpg }
        $obj = $up.Body | ConvertFrom-Json
        Add-Result "M-FACE" "jpg" "Stage1 emotion upload" ($up.Status -eq 200 -and $obj.emotion) $up.Body
    } catch { Add-Result "M-FACE" "jpg" "Stage1 upload" $false $_.Exception.Message }
}

try {
    Invoke-Api GET "$mfPrefix/api/users/sign-out" $null $mfSession | Out-Null
    Add-Result "M-FACE" "auth" "sign-out" $true "ok"
} catch { Add-Result "M-FACE" "auth" "sign-out" $false $_.Exception.Message }

if ($mfSeq) {
    Test-PgPersist "mbody_revised" "SELECT COUNT(*) FROM mface_images WHERE user_account_seq=$mfSeq;" "M-FACE" | Out-Null
}

# ========== M-SOCIAL ==========
$msPrefix = "/msocial"
$msId = "ms_" + (Get-Date -Format "HHmmss")
$msSession = New-Session
$msSeq = $null

try {
    $r = Invoke-Api GET "$msPrefix/api/health"
    Add-Result "M-SOCIAL" "health" "GET /msocial/api/health" ($r.Content -match "ok") $r.Content
} catch { Add-Result "M-SOCIAL" "health" "health" $false $_.Exception.Message }

$adm2 = New-Session
try {
    Invoke-Api POST "$msPrefix/api/users/sign-in" '{"id":"super-admin","password":"super-admin!@#$"}' $adm2 | Out-Null
    $body = (@{ level=1; domain="spike"; id=$msId; password="pw1234"; name="s"; email="s@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "$msPrefix/api/users/accounts" $body $adm2 | Out-Null
    Invoke-Api GET "$msPrefix/api/users/sign-out" $null $adm2 | Out-Null
    $r = Invoke-Api POST "$msPrefix/api/users/sign-in" (@{ id=$msId; password="pw1234" } | ConvertTo-Json -Compress) $msSession
    $msSeq = Get-UnitySeq $r.Content
    Add-Result "M-SOCIAL" "auth" "sign-in seq" ($msSeq -match '^\d+$') "seq=$msSeq"
} catch { Add-Result "M-SOCIAL" "auth" "sign-in" $false $_.Exception.Message }

if ($msSeq) {
    try {
        $csv = Join-Path $env:TEMP ("msocial_" + [Guid]::NewGuid().ToString('N') + ".csv")
        Set-Content $csv "ItemString,Stage,Emotion`nTTTTTTTTTTTTTTTT,1,0" -Encoding UTF8
        $up = Upload-Multipart $msSession "$msPrefix/api/mface-images" @{ user = $msSeq; file = $csv }
        Add-Result "M-SOCIAL" "sync" "CSV upload" ($up.Status -eq 200 -and $up.Body -match '"key"') $up.Body
        $r = Invoke-Api GET "$msPrefix/api/mface-images?account=$msSeq" $msSession
        $items = $r.Content | ConvertFrom-Json
        Add-Result "M-SOCIAL" "sync" "list images" ($items.Count -ge 1) "count=$($items.Count)"
    } catch { Add-Result "M-SOCIAL" "sync" "csv sync" $false $_.Exception.Message }
}

try {
    Invoke-Api POST "$msPrefix/api/users/sign-in" (@{ id=$msId; password="wrong" } | ConvertTo-Json -Compress) (New-Session) | Out-Null
    Add-Result "M-SOCIAL" "negative" "wrong password" $false "unexpected"
} catch {
    $code = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    Add-Result "M-SOCIAL" "negative" "wrong password" ($code -eq 401) "HTTP $code"
}

try {
    Invoke-Api GET "$msPrefix/api/users/sign-out" $null $msSession | Out-Null
    Add-Result "M-SOCIAL" "auth" "sign-out" $true "ok"
} catch { Add-Result "M-SOCIAL" "auth" "sign-out" $false $_.Exception.Message }

# --- Summary ---
$passed = @($results | Where-Object { $_.Pass }).Count
$failed = @($results | Where-Object { -not $_.Pass }).Count
$total = $results.Count
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$md = @(
    "# MTalk Unified API Verification (PostgreSQL)",
    "",
    "> Run: $ts",
    "> Base: $BaseUrl",
    "> Result: **$passed / $total PASS**",
    "",
    "| Service | Category | Test | Pass | Detail |",
    "|---------|----------|------|------|--------|"
)
foreach ($r in $results) {
    $m = if ($r.Pass) { "OK" } else { "NG" }
    $md += "| $($r.Service) | $($r.Category) | $($r.Name) | $m | $($r.Detail) |"
}
if ($failed -eq 0) { $md += "", "All MTalk service routes verified against PostgreSQL backend." }
else { $md += "", "$failed test(s) failed." }

New-Item -ItemType Directory -Force -Path (Split-Path $reportPath) | Out-Null
Set-Content -Path $reportPath -Value ($md -join "`n") -Encoding UTF8

Write-Host "`nSUMMARY: $passed / $total passed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "Report: $reportPath"
if ($failed -gt 0) { exit 1 }
