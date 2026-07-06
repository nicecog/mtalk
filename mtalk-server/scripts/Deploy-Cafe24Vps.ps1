# MTalk CAFE24 VPS 배포 스크립트
param(
    [string]$EnvFile = (Join-Path $PSScriptRoot "..\.env.cafe24"),
    [string]$RemoteDir = "/opt/mtalk"
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
$root = Split-Path $PSScriptRoot -Parent

function Invoke-Remote([string]$cmd) {
    & $plink -ssh -batch -hostkey $hostkey -pw $pw $target -P $env:CAFE24_VPS_PORT $cmd
    if ($LASTEXITCODE -ne 0) { throw "Remote command failed: $cmd" }
}

Write-Host "=== Build mtalk JAR ===" -ForegroundColor Cyan
$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-21.0.11.10-hotspot"
Get-Process java -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Push-Location $root
mvn package -DskipTests -q
Pop-Location

Write-Host "=== Prepare VPS directories ===" -ForegroundColor Cyan
Invoke-Remote "mkdir -p $RemoteDir/docker $RemoteDir/target"

Write-Host "=== Upload files ===" -ForegroundColor Cyan
$files = @(
    "$root\target\mtalk-server-1.0.0.jar",
    "$root\Dockerfile",
    "$root\docker-compose.prod.yml",
    "$root\docker\init-databases.sql",
    "$root\.env.cafe24"
)
foreach ($f in $files) {
    $name = Split-Path $f -Leaf
    $remoteName = $name
    if ($name -eq ".env.cafe24") { $remoteName = ".env" }
    elseif ($name -eq "docker-compose.prod.yml") { $remoteName = "docker-compose.yml" }
    elseif ($name -eq "mtalk-server-1.0.0.jar") { $remoteName = "target/mtalk-server-1.0.0.jar" }
    elseif ($name -eq "init-databases.sql") { $remoteName = "docker/init-databases.sql" }
    & $pscp -batch -hostkey $hostkey -pw $pw $f "${target}:${RemoteDir}/$remoteName"
    if ($LASTEXITCODE -ne 0) { throw "Upload failed: $f" }
}

Write-Host "=== Install Docker (if needed) & deploy ===" -ForegroundColor Cyan
$deployCmd = "apt-get update -qq && apt-get install -y -qq ca-certificates curl 2>/dev/null; if ! command -v docker >/dev/null 2>&1; then curl -fsSL https://get.docker.com | sh && systemctl enable docker && systemctl start docker; fi; cd /opt/mtalk && docker compose down 2>/dev/null || true; docker compose build --no-cache; docker compose up -d; sleep 15; docker compose ps; curl -sf http://127.0.0.1:8080/health; echo DEPLOY_OK"
Invoke-Remote $deployCmd

Write-Host "DEPLOY_OK on VPS" -ForegroundColor Green
