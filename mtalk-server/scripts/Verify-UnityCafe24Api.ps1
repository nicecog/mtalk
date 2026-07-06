# Unity 클라이언트 API 호출 검증 (CAFE24 VPS / hudit.cafe24.com)
param(
    [string]$BaseUrl = "http://hudit.cafe24.com:8080"
)

$ErrorActionPreference = "Continue"
$results = New-Object System.Collections.Generic.List[object]
$ts = Get-Date -Format "yyyy-MM-dd HH:mmss"
$reportPath = Join-Path $PSScriptRoot "..\docs\UNITY_CAFE24_API_VERIFICATION.md"

function Add-Result($app, $name, $pass, $detail) {
    $results.Add([PSCustomObject]@{ App = $app; Name = $name; Pass = $pass; Detail = $detail }) | Out-Null
    $m = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$m] [$app] $name - $detail"
}

function New-Session { return New-Object Microsoft.PowerShell.Commands.WebRequestSession }

function Invoke-Api($Method, $Path, $Body = $null, $Session = (New-Session)) {
    $uri = $BaseUrl.TrimEnd('/') + $Path
    if ($Method -eq "GET") {
        return Invoke-WebRequest -Uri $uri -Method Get -WebSession $Session -UseBasicParsing
    }
    return Invoke-WebRequest -Uri $uri -Method $Method -WebSession $Session -UseBasicParsing `
        -ContentType "application/json" -Body $Body
}

function Get-Seq($text) {
    $obj = $text | ConvertFrom-Json
    if ($obj.seq) { return [string]$obj.seq }
    if ($obj.success) { return "ok" }
    throw "parse failed: $text"
}

Write-Host "=== Unity-compatible API test @ $BaseUrl ===" -ForegroundColor Cyan

# M-BENCH (MBenchServerConfig production path)
$mb = New-Session
$id = "unity_mb_" + (Get-Date -Format "HHmmss")
try {
    $r = Invoke-Api GET "/mbench/api/users/accounts/id/has/$id" $null (New-Session)
    Add-Result "M-BENCH" "has=false" ($r.Content -eq "false") $r.Content
    Invoke-Api POST "/mbench/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' $mb | Out-Null
    $body = (@{ levelId=1; domain="spike"; id=$id; password="pw1234"; name="t"; email="t@t.com"; phone="0" } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/mbench/api/users/accounts" $body $mb
    Add-Result "M-BENCH" "create account" (($r.Content | ConvertFrom-Json).success) $r.Content
    Invoke-Api GET "/mbench/api/users/logout" $null $mb | Out-Null
    $r = Invoke-Api POST "/mbench/api/users/login" (@{ id=$id; password="pw1234" } | ConvertTo-Json -Compress) (New-Session)
    Add-Result "M-BENCH" "user login" (($r.Content | ConvertFrom-Json).success) $r.Content
} catch { Add-Result "M-BENCH" "flow" $false $_.Exception.Message }

# M-BODY (JsonRequest remoteUrl path)
$md = New-Session
$id2 = "s_unity_" + (Get-Date -Format "HHmmss")
try {
    $boot = New-Session
    Invoke-Api POST "/mbody/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' $boot | Out-Null
    $body = (@{ levelId=10; domain="spike"; id=$id2; password="pw1234"; name="u"; email="u@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "/mbody/api/users/accounts" $body $boot | Out-Null
    Invoke-Api GET "/mbody/api/users/logout" $null $boot | Out-Null
    $r = Invoke-Api POST "/mbody/api/users/login" (@{ id=$id2; password="pw1234" } | ConvertTo-Json -Compress) $md
    Add-Result "M-BODY" "login+seq" ([int](Get-Seq $r.Content) -gt 0) $r.Content
} catch { Add-Result "M-BODY" "flow" $false $_.Exception.Message }

# M-FACE (MFaceApiClient remoteUrl path)
$mf = New-Session
$id3 = "mf_unity_" + (Get-Date -Format "HHmmss")
try {
    $adm = New-Session
    Invoke-Api POST "/mface/api/users/sign-in" '{"id":"super-admin","password":"super-admin!@#$"}' $adm | Out-Null
    $body = (@{ level=1; domain="spike"; id=$id3; password="pw1234"; name="f"; email="f@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "/mface/api/users/accounts" $body $adm | Out-Null
    Invoke-Api GET "/mface/api/users/sign-out" $null $adm | Out-Null
    $r = Invoke-Api POST "/mface/api/users/sign-in" (@{ id=$id3; password="pw1234" } | ConvertTo-Json -Compress) $mf
    Add-Result "M-FACE" "sign-in+seq" ([int](Get-Seq $r.Content) -gt 0) $r.Content
} catch { Add-Result "M-FACE" "flow" $false $_.Exception.Message }

# M-SOCIAL (Login.cs BaseUrl path)
$ms = New-Session
$id4 = "ms_unity_" + (Get-Date -Format "HHmmss")
try {
    $adm2 = New-Session
    Invoke-Api POST "/msocial/api/users/sign-in" '{"id":"super-admin","password":"super-admin!@#$"}' $adm2 | Out-Null
    $body = (@{ level=1; domain="spike"; id=$id4; password="pw1234"; name="s"; email="s@gmail.com"; phone="000" } | ConvertTo-Json -Compress)
    Invoke-Api POST "/msocial/api/users/accounts" $body $adm2 | Out-Null
    Invoke-Api GET "/msocial/api/users/sign-out" $null $adm2 | Out-Null
    $r = Invoke-Api POST "/msocial/api/users/sign-in" (@{ id=$id4; password="pw1234" } | ConvertTo-Json -Compress) $ms
    Add-Result "M-SOCIAL" "sign-in+seq" ([int](Get-Seq $r.Content) -gt 0) $r.Content
} catch { Add-Result "M-SOCIAL" "flow" $false $_.Exception.Message }

$passed = @($results | Where-Object { $_.Pass }).Count
$total = $results.Count
$md = @("# Unity CAFE24 API Verification", "", "> $ts", "> Base: $BaseUrl", "> Result: **$passed / $total PASS**", "", "| App | Test | Pass | Detail |", "|-----|------|------|--------|")
foreach ($r in $results) {
    $md += "| $($r.App) | $($r.Name) | $(if ($r.Pass){'OK'}else{'NG'}) | $($r.Detail) |"
}
Set-Content $reportPath ($md -join "`n") -Encoding UTF8
Write-Host "`nSUMMARY: $passed / $total" -ForegroundColor $(if ($passed -eq $total) {"Green"} else {"Red"})
if ($passed -lt $total) { exit 1 }
