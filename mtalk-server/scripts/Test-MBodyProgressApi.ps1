# M-BODY player progress API test (standalone server or mtalk prefix)
# Usage:
#   .\Test-MBodyProgressApi.ps1 -BaseUrl "http://127.0.0.1:21250"
#   .\Test-MBodyProgressApi.ps1 -BaseUrl "http://127.0.0.1:8080/mbody"

param(
    [string]$BaseUrl = "http://127.0.0.1:8080/mbody",
    [string]$TestUserId = "progress_test1",
    [string]$TestPassword = "progress_test1"
)

$ErrorActionPreference = "Stop"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$results = [System.Collections.Generic.List[object]]::new()

function Write-TestResult($name, $ok, $detail) {
    $results.Add([PSCustomObject]@{ Test = $name; Pass = $ok; Detail = $detail }) | Out-Null
    $mark = if ($ok) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] $name — $detail"
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body = $null
    )
    $uri = ($BaseUrl.TrimEnd('/') + $Path)
    $params = @{
        Uri             = $uri
        Method          = $Method
        WebSession      = $session
        UseBasicParsing = $true
    }
    if ($Body) {
        $params.ContentType = "application/json"
        $params.Body = $Body
    }
    return Invoke-WebRequest @params
}

Write-Host "=== M-BODY Progress API Test ==="
Write-Host "Base URL: $BaseUrl"
Write-Host ""

$accountId = $TestUserId

try {
    $r = Invoke-Api GET "/api/health"
    Write-TestResult "GET /api/health" $true "status=$($r.StatusCode)"
} catch {
    Write-TestResult "GET /api/health" $false $_.Exception.Message
}

try {
    $r = Invoke-Api GET "/api/users/accounts/id/has/$accountId"
    $exists = $r.Content -match "true"
    Write-TestResult "GET account has" $true "exists=$exists"
} catch {
    Write-TestResult "GET account has" $false $_.Exception.Message
    $exists = $false
}

if (-not $exists) {
    try {
        Invoke-Api POST "/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' | Out-Null
        $createBody = (@{
            levelId  = 10
            domain   = "spike"
            id       = $accountId
            password = $TestPassword
            name     = $TestUserId
            email    = "$TestUserId@test.local"
            phone    = "000"
        } | ConvertTo-Json -Compress)
        Invoke-Api POST "/api/users/accounts" $createBody | Out-Null
        Invoke-Api GET "/api/users/logout" | Out-Null
        Write-TestResult "bootstrap account" $true "created $accountId"
    } catch {
        Write-TestResult "bootstrap account" $false $_.Exception.Message
    }
}

try {
    $loginBody = (@{ id = $accountId; password = $TestPassword } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/users/login" $loginBody
    Write-TestResult "POST login" $true "status=$($r.StatusCode)"
} catch {
    Write-TestResult "POST login" $false $_.Exception.Message
}

try {
    $r = Invoke-Api GET "/api/players/me/progress"
    $progress = $r.Content | ConvertFrom-Json
    Write-TestResult "GET /api/players/me/progress" $true "playTimes=$($progress.playTimes) danceStar=$($progress.danceStar)"
} catch {
    Write-TestResult "GET /api/players/me/progress" $false $_.Exception.Message
}

try {
    $putBody = (@{
        playTimes     = 4
        bodyPlayTimes = 2
        danceStar     = "TTFFFT-FFFFFFF"
    } | ConvertTo-Json -Compress)
    $r = Invoke-Api PUT "/api/players/me/progress" $putBody
    $updated = $r.Content | ConvertFrom-Json
    $ok = ($updated.playTimes -eq 4) -and ($updated.danceStar -like "TTFFFT*")
    Write-TestResult "PUT /api/players/me/progress" $ok "playTimes=$($updated.playTimes) danceStar=$($updated.danceStar)"
} catch {
    Write-TestResult "PUT /api/players/me/progress" $false $_.Exception.Message
}

try {
    $trBody = (@{
        recordedAt = "26-07-03 12:00:00"
        resultType = "Dance"
        trial      = "3"
        point      = "0"
        details    = "DanceIndex:1"
    } | ConvertTo-Json -Compress)
    $r = Invoke-Api POST "/api/players/me/training-results" $trBody
    Write-TestResult "POST /api/players/me/training-results" $true "status=$($r.StatusCode)"
} catch {
    Write-TestResult "POST /api/players/me/training-results" $false $_.Exception.Message
}

try {
    Invoke-Api GET "/api/users/logout" | Out-Null
    Invoke-Api POST "/api/users/login" '{"id":"super-admin","password":"super-admin!@#$"}' | Out-Null
    $r = Invoke-Api GET "/api/admin/progress"
    $rows = $r.Content | ConvertFrom-Json
    Write-TestResult "GET /api/admin/progress" $true "rows=$($rows.Count)"
} catch {
    Write-TestResult "GET /api/admin/progress" $false $_.Exception.Message
}

Write-Host ""
$passed = ($results | Where-Object Pass).Count
$total = $results.Count
Write-Host "=== Summary: $passed / $total passed ==="
if ($passed -lt $total) { exit 1 }
exit 0
