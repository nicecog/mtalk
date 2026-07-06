# Full M-Face API verification against MBody_Revised_server (shared local server)
param(
    [string]$BaseUrl = "http://127.0.0.1:21250"
)

$ErrorActionPreference = "Continue"
$results = New-Object System.Collections.Generic.List[object]
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$reportDir = Join-Path (Split-Path $PSScriptRoot -Parent) "docs"
$reportPath = Join-Path $reportDir "MFACE_LOCAL_API_VERIFICATION_LATEST.md"

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
    foreach ($part in ($text -split ',')) {
        if ($part -match 'seq') {
            return ($part.Split(':')[1].Replace('}', '').Replace('"', '').Trim())
        }
    }
    throw "seq parse failed: $text"
}

function New-MfaceAccountJson($id, $password, $name) {
    return (@{
        level    = 1
        domain   = "spike"
        id       = $id.ToLower()
        password = $password
        name     = $name
        email    = "$($id.ToLower())@gmail.com"
        phone    = "000"
        alias    = "null"
    } | ConvertTo-Json -Compress)
}

function Get-CookieJar($sess) {
    $jar = Join-Path $env:TEMP ("mface-full-" + [Guid]::NewGuid().ToString('N') + ".txt")
    $lines = @("# Netscape HTTP Cookie File")
    $sess.Cookies.GetCookies([Uri]$BaseUrl) | ForEach-Object {
        $lines += ("127.0.0.1`tFALSE`t/`tFALSE`t0`t" + $_.Name + "`t" + $_.Value)
    }
    Set-Content $jar -Value $lines -Encoding ascii
    return $jar
}

function Upload-MfaceMultipart($sess, $userSeq, $filePath, $fieldName) {
    $jar = Get-CookieJar $sess
    $bodyFile = Join-Path $env:TEMP ("mface-body-" + [Guid]::NewGuid().ToString('N') + ".json")
    $status = & curl.exe -sS -o $bodyFile -w "%{http_code}" -X POST ($BaseUrl.TrimEnd('/') + "/api/mface-images") `
        -F "user=$userSeq" -F "$fieldName=@$filePath" -b $jar -c $jar
    $body = if (Test-Path $bodyFile) { Get-Content $bodyFile -Raw } else { "" }
    Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue
    return @{ Status = [int]$status; Body = $body.Trim() }
}

function Parse-UnityListResponse([string]$text) {
    $items = @()
    foreach ($chunk in ($text -split '}')) {
        if ($chunk -notmatch '_FaceData\.csv|\.jpg') { continue }
        $key = ""; $seq = ""; $name = ""
        foreach ($token in ($chunk -split ',')) {
            if ($token -match '"key"') { $key = $token.Split(':')[1].Trim().Trim('"') }
            if ($token -match '"seq"') { $seq = $token.Split(':')[1].Trim() }
            if ($token -match '"name"') { $name = $token.Split(':')[1].Trim().Trim('"') }
        }
        if ($key) { $items += [PSCustomObject]@{ key = $key; seq = $seq; name = $name } }
    }
    return $items
}

function Parse-UnityEmotion([string]$emotionJson, [string]$emotionText) {
    $er = $emotionJson | ConvertFrom-Json
    $s = $er.emotion.Replace(" ", "")
    $t = $s.Split(',')
    $em = @{
        Neutral = [double]$t[0].Split(':')[1]
        Happy   = [double]$t[1].Split(':')[1]
        Sad     = [double]$t[2].Split(':')[1]
        Anger   = [double]$t[6].Split(':')[1]
    }
    $THR = 0.15
    $emotion = -1
    if ($em.Happy -ge $em.Anger -and $em.Happy -ge $em.Sad -and $em.Happy -ge $em.Neutral) { $emotion = 0 }
    elseif ($em.Neutral -ge $em.Happy -and $em.Neutral -ge $em.Sad -and $em.Neutral -ge $em.Anger -and $em.Anger -le $THR*1.5 -and $em.Sad -le $THR*1.5) { $emotion = 1 }
    elseif ($em.Sad -ge $em.Happy -and $em.Sad -ge $em.Neutral -and $em.Sad -ge $em.Anger) { $emotion = 2 }
    elseif ($em.Anger -ge $em.Happy -and $em.Anger -ge $em.Neutral -and $em.Anger -ge $em.Sad) { $emotion = 3 }
    if ($emotionText -eq "Happy" -and $em.Happy -ge $THR) { $emotion = 0 }
    elseif ($emotionText -eq "Neutral" -and $em.Neutral -ge $THR -and $em.Anger -le $THR*1.5 -and $em.Sad -le $THR*1.5) { $emotion = 1 }
    elseif ($emotionText -eq "Sad" -and $em.Sad -ge $THR) { $emotion = 2 }
    elseif ($emotionText -eq "Anger" -and $em.Anger -ge $THR) { $emotion = 3 }
    return $emotion
}

Write-Host "=== M-Face Full API Verification (MBody Local Server) ==="
Write-Host "Base: $BaseUrl"

# A. Connectivity
try {
    $r = Invoke-Api GET "/api/health"
    Add-Result "A-connect" "server health" ($r.StatusCode -eq 200) ("HTTP " + $r.StatusCode + " " + $r.Content) "MFaceApiClient.GetBaseUrl"
} catch {
    Add-Result "A-connect" "server health" $false $_.Exception.Message ""
    exit 2
}

$verifyId = "mf" + (Get-Date -Format 'HHmmss')
$accountId = $verifyId.ToLower()
$csvName = "${accountId}_FaceData.csv"
$today = (Get-Date -Format 'yyyy-MM-dd')

# B. has=false
try {
    $r = Invoke-Api GET "/api/users/accounts/id/has/$accountId"
    Add-Result "B-auth" "new account has=false" ((-not $r.Content.Contains("true")) -and $r.StatusCode -eq 200) $r.Content "Login.AdminLogin L73"
} catch {
    Add-Result "B-auth" "new account has=false" $false $_.Exception.Message ""
}

# C. Bootstrap (super-admin sign-in -> create -> sign-out)
$boot = New-Session
try {
    $r = Invoke-Api POST "/api/users/sign-in" '{"id":"super-admin","password":"super-admin!@#$"}' $boot
    $hasCookie = ($boot.Cookies.GetCookies([Uri]$BaseUrl) | Where-Object { $_.Name -eq 'JSESSIONID' }).Count -gt 0
    Add-Result "C-bootstrap" "super-admin sign-in" ($r.StatusCode -eq 200 -and $hasCookie) "JSESSIONID=$hasCookie" "Login L84 sign-in alias"
} catch { Add-Result "C-bootstrap" "super-admin sign-in" $false $_.Exception.Message "" }

try {
    $createJson = New-MfaceAccountJson $accountId "mfacepw1" "mfaceuser"
    $r = Invoke-Api POST "/api/users/accounts" $createJson $boot
    $created = $r.Content | ConvertFrom-Json
    Add-Result "C-bootstrap" "create account (level field)" ($r.StatusCode -eq 200 -and $created.id -eq $accountId) $r.Content "Login L86 level alias"
} catch { Add-Result "C-bootstrap" "create account (level field)" $false $_.Exception.Message "" }

try {
    $r = Invoke-Api GET "/api/users/sign-out" $boot
    Add-Result "C-bootstrap" "bootstrap sign-out" ($r.StatusCode -eq 200) $r.Content "Login.LogOut sign-out alias"
} catch { Add-Result "C-bootstrap" "bootstrap sign-out" $false $_.Exception.Message "" }

# D. User sign-in + Unity seq parse
$userSession = New-Session
$userSeq = $null
try {
    $loginJson = (@{ id = $accountId; password = "mfacepw1" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/sign-in" $loginJson $userSession
    $userSeq = Get-UnitySeq $r.Content
    Add-Result "D-auth" "user sign-in + seq parse" ($userSeq -match '^\d+$') "seq=$userSeq" "Login.sendPost L100-118"
} catch { Add-Result "D-auth" "user sign-in + seq parse" $false $_.Exception.Message "" }

# E. has=true
try {
    $r = Invoke-Api GET "/api/users/accounts/id/has/$accountId"
    Add-Result "E-auth" "existing account has=true" ($r.Content.Contains("true")) $r.Content "Login existing branch"
} catch { Add-Result "E-auth" "existing account has=true" $false $_.Exception.Message "" }

# F. First CSV upload (Login.Upload - new user)
$csvPath = Join-Path $env:TEMP $csvName
$csvFileSeq = $null
$csvKey = $null
if ($userSeq) {
    try {
        Set-Content $csvPath "Day_Count,Last_Login,Item`n1,$today,FFFFFFFFFFFFFFFF`nDate,Time,Log" -Encoding UTF8
        $up = Upload-MfaceMultipart $userSession $userSeq $csvPath $csvName
        $obj = $up.Body | ConvertFrom-Json
        $csvFileSeq = $obj.seq
        Add-Result "F-csv" "initial FaceData.csv upload" ($up.Status -eq 200 -and $obj.name -eq $csvName) $up.Body "Login.Upload L141-157"
    } catch { Add-Result "F-csv" "initial FaceData.csv upload" $false $_.Exception.Message "" }

    # G. List + Unity-style parse
    try {
        $r = Invoke-Api GET "/api/mface-images?&account=$userSeq" $userSession
        $parsed = Parse-UnityListResponse $r.Content
        $csvItem = $parsed | Where-Object { $_.name -eq $csvName } | Select-Object -First 1
        $csvKey = $csvItem.key
        Add-Result "G-list" "list by account + Unity parse" ($csvKey -and $csvItem.seq) "key=$csvKey seq=$($csvItem.seq)" "Login.fileUpdate L172-189"
    } catch { Add-Result "G-list" "list by account + Unity parse" $false $_.Exception.Message "" }

    # H. Download CSV
    if ($csvKey) {
        try {
            $dlJar = Get-CookieJar $userSession
            $dlFile = Join-Path $env:TEMP "mface-dl.csv"
            $st = & curl.exe -sS -o $dlFile -w "%{http_code}" ($BaseUrl + "/api/mface-images/$csvKey") -b $dlJar
            $dlText = Get-Content $dlFile -Raw -Encoding UTF8
            $hasHeader = $dlText -match "Day_Count" -and $dlText -match "FFFFFFFFFFFFFFFF"
            Add-Result "H-download" "download CSV by key" ([int]$st -eq 200 -and $hasHeader) ("bytes=" + (Get-Item $dlFile).Length) "Login.fileUpdate L192"
        } catch { Add-Result "H-download" "download CSV by key" $false $_.Exception.Message "" }
    }

    # I. log append (Login.log)
    try {
        $logLine = "$today,12.00.00.000,{Stage:1;Point:(10/12);Face:2}"
        $dl = Invoke-Api GET "/api/mface-images?&account=$userSeq" $userSession
        $parsed = Parse-UnityListResponse $dl.Content
        $item = $parsed | Where-Object { $_.name -eq $csvName } | Select-Object -First 1
        $oldSeq = $item.seq
        Add-Content $csvPath $logLine -Encoding UTF8
        $up = Upload-MfaceMultipart $userSession $userSeq $csvPath $csvName
        $del = Invoke-Api DELETE "/api/mface-images/$oldSeq" $userSession
        Add-Result "I-log" "log append + reupload + delete old" ($up.Status -eq 200 -and $del.StatusCode -eq 200) "deleted seq=$oldSeq" "Login.log L251-295"
    } catch { Add-Result "I-log" "log append + reupload + delete old" $false $_.Exception.Message "" }

    # J. itemUpdate (ItemString change)
    try {
        $newItem = "TTTTTTTTTTTTTTTT"
        Set-Content $csvPath "Day_Count,Last_Login,Item`n3,$today,$newItem`nDate,Time,Log`n$logLine" -Encoding UTF8
        $dl = Invoke-Api GET "/api/mface-images?&account=$userSeq" $userSession
        $item = (Parse-UnityListResponse $dl.Content | Where-Object { $_.name -eq $csvName } | Select-Object -First 1)
        $oldSeq = $item.seq
        $up = Upload-MfaceMultipart $userSession $userSeq $csvPath $csvName
        $newObj = $up.Body | ConvertFrom-Json
        Invoke-Api DELETE "/api/mface-images/$oldSeq" $userSession | Out-Null
        $dlJar = Get-CookieJar $userSession
        $dlFile = Join-Path $env:TEMP "mface-item.csv"
        $st = & curl.exe -sS -o $dlFile -w "%{http_code}" ($BaseUrl + "/api/mface-images/$($newObj.key)") -b $dlJar
        $dlText = Get-Content $dlFile -Raw -Encoding UTF8
        $itemOk = ([int]$st -eq 200) -and ($dlText -match $newItem)
        Add-Result "J-item" "itemUpdate ItemString" ($up.Status -eq 200 -and $itemOk) "ItemString=$newItem newKey=$($newObj.key)" "Login.itemUpdate L303-351"
    } catch { Add-Result "J-item" "itemUpdate ItemString" $false $_.Exception.Message "" }

    # K. Stage1 JPG + emotion (WebCam.Upload)
    foreach ($emo in @("Happy", "Neutral", "Sad", "Anger")) {
        try {
            $jpgPath = Join-Path $env:TEMP "stage1_$emo.jpg"
            [IO.File]::WriteAllBytes($jpgPath, [byte[]](0xFF,0xD8,0xFF,0xD9))
            $fname = "${accountId}_Stage1_test_$emo.jpg"
            $bodyFile = Join-Path $env:TEMP ("mface-jpg-$emo.json")
            $jar = Get-CookieJar $userSession
            $status = & curl.exe -sS -o $bodyFile -w "%{http_code}" -X POST ($BaseUrl + "/api/mface-images") `
                -F "user=$userSeq" -F "file=@$jpgPath;filename=$fname" -b $jar -c $jar
            $body = (Get-Content $bodyFile -Raw).Trim()
            Remove-Item $bodyFile -Force -ErrorAction SilentlyContinue
            $parsedEmo = Parse-UnityEmotion $body $emo
            $expected = @{ Happy = 0; Neutral = 1; Sad = 2; Anger = 3 }[$emo]
            Add-Result "K-stage1" "JPG $emo emotion classify" ([int]$status -eq 200 -and $parsedEmo -eq $expected) "parsed=$parsedEmo expected=$expected body=$body" "WebCam.Upload L234-287"
        } catch {
            Add-Result "K-stage1" "JPG $emo emotion classify" $false $_.Exception.Message "WebCam.Upload"
        }
    }

    # L. Stage2 JPG with correct user seq (WebCamStage2 fix verification)
    try {
        $jpgPath = Join-Path $env:TEMP "stage2_Happy.jpg"
        [IO.File]::WriteAllBytes($jpgPath, [byte[]](0xFF,0xD8,0xFF,0xD9))
        $fname = "${accountId}_Stage2_test_Happy.jpg"
        $up = Upload-MfaceMultipart $userSession $userSeq $jpgPath $fname
        $obj = $up.Body | ConvertFrom-Json
        $seqMatch = [string]$obj.user_account_seq -eq $userSeq
        Add-Result "L-stage2" "Stage2 upload user=seq" ($up.Status -eq 200 -and $seqMatch -and $obj.emotion) "user_account_seq=$($obj.user_account_seq)" "WebCamStage2.Upload L251"
    } catch { Add-Result "L-stage2" "Stage2 upload user=seq" $false $_.Exception.Message "" }
}

# M. sign-out
try {
    $r = Invoke-Api GET "/api/users/sign-out" $userSession
    Add-Result "M-auth" "user sign-out" ($r.StatusCode -eq 200) $r.Content "Login.LogOut"
} catch { Add-Result "M-auth" "user sign-out" $false $_.Exception.Message "" }

# N. Negative tests
try {
    $bad = Invoke-Api POST "/api/users/sign-in" '{"id":"super-admin","password":"wrong"}' (New-Session)
    Add-Result "N-negative" "wrong password" $false ("unexpected " + $bad.StatusCode)
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Add-Result "N-negative" "wrong password" ($code -eq 401) ("HTTP " + $code) ""
}

try {
    $bad = Invoke-Api POST "/api/users/accounts" (New-MfaceAccountJson "x" "y" "z") (New-Session)
    Add-Result "N-negative" "create without admin" $false ("unexpected " + $bad.StatusCode)
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    Add-Result "N-negative" "create without admin" ($code -eq 403) ("HTTP " + $code) ""
}

# O. Coexistence: MBody login alias still works on same server
try {
    $r = Invoke-Api POST "/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' (New-Session)
    Add-Result "O-shared" "MBody /login alias on same server" ($r.StatusCode -eq 200) "shared auth service" "JsonRequest MBody"
} catch { Add-Result "O-shared" "MBody /login alias on same server" $false $_.Exception.Message "" }

# P. DB persist (MySQL docker or API fallback)
if ($userSeq) {
    try {
        $dbOk = $false
        $dbDetail = ""
        try {
            $sql = "SELECT COUNT(*) FROM user_accounts WHERE login_id='$accountId'; SELECT COUNT(*) FROM mface_images WHERE user_account_seq=$userSeq;"
            $dbOut = docker exec mbody-revised-mysql mysql -umbody -pmbody1234 mbody_revised -N -e $sql 2>$null
            $lines = @($dbOut -split "`r?`n" | Where-Object { $_ -match '^\d+$' })
            if ($lines.Count -ge 2 -and [int]$lines[0] -ge 1 -and [int]$lines[1] -ge 1) {
                $dbOk = $true
                $dbDetail = "mysql accounts=" + $lines[0] + " images=" + $lines[1]
            }
        } catch {}
        if (-not $dbOk) {
            $listR = Invoke-WebRequest "$($BaseUrl.TrimEnd('/'))/api/mface-images?account=$userSeq" -UseBasicParsing
            $items = $listR.Content | ConvertFrom-Json
            $dbOk = ($items.Count -ge 1)
            $dbDetail = "api-fallback images=" + $items.Count + " (H2 or no docker mysql)"
        }
        Add-Result "P-db" "data persist (accounts + mface_images)" $dbOk $dbDetail ""
    } catch { Add-Result "P-db" "data persist" $false $_.Exception.Message "" }
}

$passed = @($results | Where-Object { $_.Pass }).Count
$failed = @($results | Where-Object { -not $_.Pass }).Count
$total = $results.Count
$ts = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$md = @()
$md += "# M-Face Local API Verification (MBody Shared Server)"
$md += ""
$md += "> Run: $ts"
$md += "> Server: MBody_Revised_server @ $BaseUrl"
$md += "> Result: **$passed / $total PASS**"
$md += ""
$md += "## Unity API Coverage"
$md += ""
$md += "| Unity Class | API | Flow |"
$md += "|-------------|-----|------|"
$md += "| Login | GET /api/users/accounts/id/has/{id} | AdminLogin |"
$md += "| Login | POST /api/users/sign-in | bootstrap + user login |"
$md += "| Login | POST /api/users/accounts | new account (level field) |"
$md += "| Login | GET /api/users/sign-out | logout |"
$md += "| Login | GET/POST/DELETE /api/mface-images | CSV sync, log, itemUpdate |"
$md += "| WebCam | POST /api/mface-images | Stage1 JPG + emotion |"
$md += "| WebCamStage2 | POST /api/mface-images | Stage2 JPG + user seq |"
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
if ($failed -eq 0) { $md += "All M-Face flows verified against MBody local shared server." }
else { $md += "$failed test(s) failed." }

Set-Content -Path $reportPath -Value ($md -join "`n") -Encoding UTF8
Write-Host ""
Write-Host "SUMMARY: $passed / $total passed"
Write-Host "Report: $reportPath"
if ($failed -gt 0) { exit 1 }
exit 0
