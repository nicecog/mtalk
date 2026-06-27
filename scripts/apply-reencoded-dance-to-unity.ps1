# Copy re-encoded MP4s from Build/ReencodedDance into Assets/DanceVideo (same filenames, keeps .meta).
param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\Build\ReencodedDance'),
    [string]$DestRoot = (Join-Path $PSScriptRoot '..\Assets\DanceVideo'),
    [string]$BackupRoot = (Join-Path $PSScriptRoot '..\Build\DanceVideoBackup'),
    [switch]$Execute
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$SourceRoot = [System.IO.Path]::GetFullPath($SourceRoot)
$DestRoot = (Resolve-Path $DestRoot).Path
$BackupRoot = [System.IO.Path]::GetFullPath($BackupRoot)

$encoded = @(Get-ChildItem -LiteralPath $SourceRoot -Filter '*_720p.mp4' -Recurse -File | Sort-Object FullName)
if ($encoded.Count -eq 0) {
    Write-Error "No *_720p.mp4 files under $SourceRoot. Run reencode-dance-mp4.ps1 -Execute first."
}

$destMp4Count = @(Get-ChildItem -LiteralPath $DestRoot -Filter '*.mp4' -Recurse -File).Count
if ($encoded.Count -lt $destMp4Count) {
    Write-Warning "Only $($encoded.Count) re-encoded files for $destMp4Count originals. Continue only if intentional."
}

Write-Host '=== Apply Re-encoded Dance MP4s to Unity ==='
Write-Host "From:   $SourceRoot"
Write-Host "To:     $DestRoot"
Write-Host "Backup: $BackupRoot"
Write-Host "Files:  $($encoded.Count)"
Write-Host "Mode:   $(if ($Execute) { 'APPLY' } else { 'DRY-RUN' })"
Write-Host ''

$index = 0
foreach ($file in $encoded) {
    $index++
    $relative720 = $file.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $relative = $relative720 -replace '_720p\.mp4$', '.mp4'
    $destPath = Join-Path $DestRoot $relative
    $srcMb = [math]::Round($file.Length / 1MB, 1)

    if (-not (Test-Path -LiteralPath $destPath)) {
        Write-Warning ("[{0}/{1}] MISSING dest: {2}" -f $index, $encoded.Count, $relative)
        continue
    }

    $destMb = [math]::Round((Get-Item -LiteralPath $destPath).Length / 1MB, 1)
    Write-Host ("[{0}/{1}] {2,6} -> {3,6} MB  {4}" -f $index, $encoded.Count, $destMb, $srcMb, $relative)

    if (-not $Execute) { continue }

    $backupPath = Join-Path $BackupRoot $relative
    $backupDir = Split-Path $backupPath -Parent
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

    if (-not (Test-Path -LiteralPath $backupPath)) {
        Copy-Item -LiteralPath $destPath -Destination $backupPath -Force
    }

    Copy-Item -LiteralPath $file.FullName -Destination $destPath -Force

    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $destPath 2>$null | Out-Null
    $probeOk = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $prevEap
    if (-not $probeOk) {
        Write-Warning "Corrupt after copy: $relative (restoring backup)"
        Copy-Item -LiteralPath $backupPath -Destination $destPath -Force
    }
}

if (-not $Execute) {
    Write-Host ''
    Write-Host 'Dry-run only. Apply with: .\scripts\apply-reencoded-dance-to-unity.ps1 -Execute'
}
