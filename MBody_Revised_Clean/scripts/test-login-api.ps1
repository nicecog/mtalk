$base = 'http://hudit.cafe24.com:8080/mbody'
$id = 'hudit1'
$pw = 'hudit!'
$badPw = 'wrongpassword'

function Test-Login {
    param([string]$Label, [string]$UserId, [string]$Password)
    $body = @{ id = $UserId; password = $Password } | ConvertTo-Json -Compress
    try {
        $r = Invoke-WebRequest -Uri ($base + '/api/users/login') -Method POST -Body $body -ContentType 'application/json' -UseBasicParsing -TimeoutSec 30
        $parsed = $r.Content | ConvertFrom-Json
        $seq = if ($null -ne $parsed.seq) { [int]$parsed.seq } else { 0 }
        [PSCustomObject]@{
            Test   = $Label
            Status = [int]$r.StatusCode
            Seq    = $seq
            Ok     = ($r.StatusCode -eq 200 -and $seq -gt 0)
            Body   = $r.Content.Substring(0, [Math]::Min(120, $r.Content.Length))
        }
    }
    catch {
        $resp = $_.Exception.Response
        $code = if ($resp) { [int]$resp.StatusCode } else { 0 }
        $text = ''
        if ($resp) {
            $sr = New-Object IO.StreamReader($resp.GetResponseStream())
            $text = $sr.ReadToEnd()
        }
        [PSCustomObject]@{
            Test   = $Label
            Status = $code
            Seq    = 0
            Ok     = $false
            Body   = if ($text.Length -gt 0) { $text.Substring(0, [Math]::Min(120, $text.Length)) } else { $_.Exception.Message }
        }
    }
}

function Test-HasAccount {
    param([string]$UserId)
    $r = Invoke-WebRequest -Uri ($base + '/api/users/accounts/id/has/' + $UserId) -UseBasicParsing -TimeoutSec 30
    [PSCustomObject]@{ Test = 'has_account'; UserId = $UserId; Status = [int]$r.StatusCode; Body = $r.Content }
}

Write-Host '=== Login API Tests ==='
Test-HasAccount -UserId $id | Format-List
Test-Login -Label 'valid_s_hudit1' -UserId $id -Password $pw | Format-List
Test-Login -Label 'wrong_password' -UserId $id -Password $badPw | Format-List
Test-Login -Label 'raw_hudit1_no_prefix' -UserId 'hudit1' -Password $pw | Format-List
