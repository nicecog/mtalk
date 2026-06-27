# Set Android texture import overrides for UI PNGs (ASTC 6x6, max 1024).
param(
    [string]$Root = (Join-Path $PSScriptRoot '..\Assets\Images'),
    [int]$MaxSize = 1024,
    [switch]$Execute
)

$ErrorActionPreference = 'Stop'

$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$projectRoot = Split-Path $PSScriptRoot -Parent
$method = 'SetAndroidTextureCompression.Apply'

if (-not (Test-Path $unity)) {
    Write-Error "Unity editor not found: $unity"
}

Write-Host '=== Android PNG ASTC (Editor batch) ==='
Write-Host "Root:    $Root"
Write-Host "MaxSize: $MaxSize"
Write-Host "Mode:    $(if ($Execute) { 'APPLY via Unity' } else { 'DRY-RUN (pass -Execute)' })"

if (-not $Execute) {
    $pngCount = (Get-ChildItem -LiteralPath $Root -Filter '*.png' -Recurse -File).Count
    Write-Host "PNG files under Images: $pngCount"
    Write-Host 'Run: .\scripts\set-texture-android-astc.ps1 -Execute'
    exit 0
}

$log = Join-Path $projectRoot 'Build\texture-astc.log'
New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

$proc = Start-Process -FilePath $unity -PassThru -Wait -NoNewWindow -ArgumentList @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', $projectRoot,
    '-executeMethod', $method,
    '-logFile', $log
)

if ($proc.ExitCode -ne 0) {
    if (Test-Path $log) { Get-Content $log -Tail 30 }
    throw "ASTC batch failed (exit $($proc.ExitCode))"
}

if (Test-Path $log) {
    Select-String -Path $log -Pattern '\[SetAndroidTextureCompression\]' | ForEach-Object { $_.Line }
}

Write-Host 'Done.'
