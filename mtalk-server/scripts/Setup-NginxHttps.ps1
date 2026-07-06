# nginx + Let's Encrypt HTTPS reverse proxy for hudit.cafe24.com
# mtalk continues on :8080 — existing Unity/API clients are unaffected.
param(
    [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env.cafe24"),
    [string]$CertEmail = "admin@hudit.cafe24.com",
    [switch]$SkipCertbot
)

$ErrorActionPreference = "Stop"

function Load-Env($path) {
    Get-Content $path | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            Set-Item -Path "env:$($matches[1].Trim())" -Value $matches[2].Trim()
        }
    }
}
Load-Env $EnvFile

$plink = "C:\Program Files\PuTTY\plink.exe"
$pscp = "C:\Program Files\PuTTY\pscp.exe"
$hostkey = $env:CAFE24_VPS_HOSTKEY
$target = "${env:CAFE24_VPS_USER}@${env:CAFE24_VPS_HOST}"
$pw = $env:CAFE24_VPS_PASSWORD
$domain = $env:CAFE24_VPS_DOMAIN
$root = Split-Path $PSScriptRoot -Parent
$nginxConf = Join-Path $root "deploy\nginx\hudit.cafe24.com.conf"

function Invoke-Remote([string]$cmd) {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $out = & $plink -ssh -batch -hostkey $hostkey -pw $pw $target -P $env:CAFE24_VPS_PORT $cmd 2>&1
    $ErrorActionPreference = $prev
    $text = ($out | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Remote command failed ($LASTEXITCODE): $cmd`n$text"
    }
    return $text
}

if (-not (Test-Path $nginxConf)) {
    throw "Missing nginx config: $nginxConf"
}

Write-Host "=== Upload nginx site config ===" -ForegroundColor Cyan
& $pscp -batch -hostkey $hostkey -pw $pw $nginxConf "${target}:/tmp/hudit.cafe24.com.conf"
if ($LASTEXITCODE -ne 0) { throw "Upload failed" }

Write-Host "=== Install nginx + certbot ===" -ForegroundColor Cyan
$installCmd = "export DEBIAN_FRONTEND=noninteractive; apt-get update -qq; apt-get install -y -qq nginx certbot python3-certbot-nginx; mkdir -p /etc/nginx/sites-available /etc/nginx/sites-enabled; mv /tmp/hudit.cafe24.com.conf /etc/nginx/sites-available/$domain; ln -sf /etc/nginx/sites-available/$domain /etc/nginx/sites-enabled/$domain; rm -f /etc/nginx/sites-enabled/default; nginx -t; systemctl enable nginx; systemctl restart nginx; echo NGINX_OK"
Invoke-Remote $installCmd

if (-not $SkipCertbot) {
    Write-Host "=== Obtain SSL certificate (Let's Encrypt) ===" -ForegroundColor Cyan
    $certCmd = "certbot --nginx -d $domain --non-interactive --agree-tos --email $CertEmail --redirect --no-eff-email 2>&1; systemctl reload nginx; echo CERTBOT_OK"
    try {
        Invoke-Remote $certCmd
    } catch {
        Write-Host "Certbot failed — HTTP proxy may still work on port 80." -ForegroundColor Yellow
        Write-Host $_.Exception.Message
    }
}

Write-Host "=== Verify endpoints ===" -ForegroundColor Cyan

function Test-Url([string]$Name, [string]$Url) {
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20
        Write-Host "[PASS] $Name -> $($r.StatusCode)" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "[FAIL] $Name -> $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$ok = $true
$ok = (Test-Url "mtalk direct :8080/health" "http://${domain}:8080/health") -and $ok
$ok = (Test-Url "nginx HTTP /health" "http://${domain}/health") -and $ok
$ok = (Test-Url "nginx HTTPS /health" "https://${domain}/health") -and $ok
$ok = (Test-Url "HUDIT portal HTTPS" "https://${domain}/") -and $ok

if (-not $ok) {
    throw "One or more verification checks failed"
}

Write-Host ""
Write-Host "HTTPS proxy ready:" -ForegroundColor Green
Write-Host "  https://${domain}/          (HUDIT portal)"
Write-Host "  https://${domain}/health    (mtalk health)"
Write-Host "  http://${domain}:8080/      (legacy — unchanged)"
