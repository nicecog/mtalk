# Repair corrupt Unity DanceVideo MP4s from Build/ReencodedDance counterparts.
param(
    [string]$UnityRoot = (Join-Path $PSScriptRoot '..\Assets\DanceVideo'),
    [string]$EncodedRoot = (Join-Path $PSScriptRoot '..\Build\ReencodedDance')
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$UnityRoot = (Resolve-Path $UnityRoot).Path
$EncodedRoot = [System.IO.Path]::GetFullPath($EncodedRoot)

if (-not (Get-Command ffprobe -ErrorAction SilentlyContinue)) {
    Write-Error 'ffprobe not found in PATH.'
}

function Test-Mp4Readable {
    param([string]$Path)
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $Path 2>$null | Out-Null
    $ok = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $prevEap
    return $ok
}

$unityFiles = Get-ChildItem -LiteralPath $UnityRoot -Filter '*.mp4' -Recurse -File
$repaired = 0
$bad = 0

foreach ($file in $unityFiles) {
    if (Test-Mp4Readable -Path $file.FullName) { continue }
    $bad++
    $relative = $file.FullName.Substring($UnityRoot.Length).TrimStart('\', '/')
    $encodedPath = Join-Path $EncodedRoot ($relative -replace '\.mp4$', '_720p.mp4')
    if (-not (Test-Path -LiteralPath $encodedPath)) {
        Write-Warning "No encoded source for corrupt file: $relative"
        continue
    }
    if (-not (Test-Mp4Readable -Path $encodedPath)) {
        Write-Warning "Encoded source also corrupt: $encodedPath"
        continue
    }
    Copy-Item -LiteralPath $encodedPath -Destination $file.FullName -Force
    if (Test-Mp4Readable -Path $file.FullName) {
        $repaired++
        Write-Host "REPAIRED: $relative"
    } else {
        Write-Warning "Repair failed: $relative"
    }
}

Write-Host "Corrupt found: $bad, repaired: $repaired"
if ($bad -gt $repaired) { exit 1 }
exit 0
