# Quality-first H.264 re-encode for DanceVideo MP4s.
# Default: dry-run. Use -Execute to write outputs (does not replace originals).
param(
    [string]$SourceRoot = (Join-Path $PSScriptRoot '..\Assets\DanceVideo'),
    [string]$OutputRoot = (Join-Path $PSScriptRoot '..\Build\ReencodedDance'),
    [int]$Height = 720,
    [int]$Crf = 20,
    [string]$Preset = 'slow',
    [int]$AudioKbps = 160,
    [int]$Limit = 0,
    [switch]$Execute,
    [switch]$ReplaceInPlace,
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$SourceRoot = (Resolve-Path $SourceRoot).Path
$OutputRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\Build\ReencodedDance'))

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error 'ffmpeg not found in PATH. Install ffmpeg and retry.'
}

$mp4Files = @(Get-ChildItem -LiteralPath $SourceRoot -Filter '*.mp4' -Recurse -File | Sort-Object FullName)
if ($Limit -gt 0) {
    $mp4Files = $mp4Files | Select-Object -First $Limit
}
if ($mp4Files.Count -eq 0) {
    Write-Warning "No MP4 files under $SourceRoot"
    exit 0
}

function Invoke-FfmpegEncode {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$Height,
        [int]$Crf,
        [string]$Preset,
        [int]$AudioKbps
    )

    $ffmpegArgs = @(
        '-y',
        '-hide_banner',
        '-loglevel', 'warning',
        '-nostats',
        '-i', $InputPath,
        '-vf', "scale=-2:${Height}:flags=lanczos",
        '-c:v', 'libx264',
        '-preset', $Preset,
        '-crf', "$Crf",
        '-profile:v', 'high',
        '-level', '4.1',
        '-pix_fmt', 'yuv420p',
        '-c:a', 'aac',
        '-b:a', "${AudioKbps}k",
        '-movflags', '+faststart',
        $OutputPath
    )

    # Direct invocation preserves Unicode paths and spaces (Start-Process breaks on spaces).
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & ffmpeg @ffmpegArgs 2>$null
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevEap

    if ($exitCode -ne 0) {
        throw "ffmpeg exit code $exitCode for: $InputPath"
    }
}

Write-Host '=== MBody Dance MP4 Re-encode (quality-first) ==='
Write-Host "Source:  $SourceRoot"
Write-Host "Output:  $OutputRoot"
Write-Host "Profile: ${Height}p H.264 CRF $Crf preset=$Preset AAC ${AudioKbps}k"
Write-Host "Files:   $($mp4Files.Count)"
Write-Host "Mode:    $(if ($Execute) { if ($ReplaceInPlace) { 'REPLACE IN PLACE' } else { 'WRITE to OutputRoot' } } else { 'DRY-RUN' })"
Write-Host ''

$index = 0
foreach ($file in $mp4Files) {
    $index++
    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    if ($ReplaceInPlace) {
        $finalPath = $file.FullName
    } else {
        $finalPath = Join-Path $OutputRoot ($relative -replace '\.mp4$', '_720p.mp4')
    }

    $outDir = Split-Path $finalPath -Parent
    $sizeMb = [math]::Round($file.Length / 1MB, 1)
    Write-Host ("[{0}/{1}] [{2,6} MB] {3}" -f $index, $mp4Files.Count, $sizeMb, $relative)

    if (-not $Execute) { continue }

    if ($SkipExisting -and (Test-Path -LiteralPath $finalPath)) {
        $newSize = [math]::Round((Get-Item -LiteralPath $finalPath).Length / 1MB, 1)
        Write-Host "         SKIP (exists) -> $newSize MB"
        continue
    }

    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $tempOut = if ($ReplaceInPlace) { "$($file.FullName).tmp.mp4" } else { $finalPath }

    try {
        Invoke-FfmpegEncode -InputPath $file.FullName -OutputPath $tempOut -Height $Height -Crf $Crf -Preset $Preset -AudioKbps $AudioKbps
        if ($ReplaceInPlace) {
            Move-Item -LiteralPath $tempOut -Destination $file.FullName -Force
        }
        $newSize = [math]::Round((Get-Item -LiteralPath $finalPath).Length / 1MB, 1)
        Write-Host "         OK -> $newSize MB"
    }
    catch {
        if (Test-Path -LiteralPath $tempOut) {
            Remove-Item -LiteralPath $tempOut -Force -ErrorAction SilentlyContinue
        }
        Write-Error $_
    }
}

if (-not $Execute) {
    Write-Host ''
    Write-Host 'Dry-run only. Examples:'
    Write-Host '  .\scripts\reencode-dance-mp4.ps1 -Execute -Limit 1   # sample 1 file'
    Write-Host '  .\scripts\reencode-dance-mp4.ps1 -Execute            # all files'
}
