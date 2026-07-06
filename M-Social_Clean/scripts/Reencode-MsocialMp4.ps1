#Requires -Version 5.1
<#
.SYNOPSIS
  Re-encode M-Social MP4 assets from msocial_mp4 backup, then replace Assets originals.
  Backup (msocial_mp4/) is never modified.
#>
param(
    [string]$ProjectRoot = "F:\PROJECT\Unity_Projects\M-Social_Clean",
    [ValidateSet("all", "StreamingAssets", "Movie", "Animations")]
    [string]$Category = "all",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$BackupRoot   = Join-Path $ProjectRoot "msocial_mp4"
$AssetsRoot   = Join-Path $ProjectRoot "Assets"
$OptimizedRoot = Join-Path $BackupRoot "optimized"
$Manifest     = Join-Path $BackupRoot "MSOCIAL_MP4_MANIFEST.csv"
$LogFile      = Join-Path $BackupRoot "REENCODE_LOG.csv"

function Get-CrfForFile {
    param([string]$Category, [string]$RelativePath, [string]$FileName, [string]$SizeMB)
    switch ($Category) {
        "Animations" { return 26 }
        "Movie"      { return 24 }
        "StreamingAssets" {
            # Unicode escapes avoid script encoding issues on Windows
            if ($FileName -match "\uCD94\uCD94\uAE30|\uBC14\uB514") { return 23 }  # dance / body
            if ($FileName -match "\uD638\uD765|\uC22B\uC790\uC138\uAE30") { return 25 }  # breath / count
            if ($FileName -match "\uC0AC\uC6B4\uB4DC") { return 24 }  # sound
            if ($FileName -match 'Band|Fly With|Ready And|Wind Maker') { return 23 }
            if ([double]$SizeMB -ge 40) { return 23 }
            return 24
        }
        default { return 24 }
    }
}

function Test-HasAudio {
    param([string]$InputPath)
    $probe = & ffprobe -v error -select_streams a:0 -show_entries stream=codec_type -of csv=p=0 "$InputPath" 2>$null
    return ($probe -match "audio")
}

function Invoke-Reencode {
    param(
        [string]$InputPath,
        [string]$OutputPath,
        [int]$Crf
    )
    $outDir = Split-Path $OutputPath -Parent
    if (-not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    }

    $vf = "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2"
    $args = @(
        "-y", "-hide_banner", "-loglevel", "error",
        "-i", $InputPath,
        "-vf", $vf,
        "-c:v", "libx264", "-profile:v", "main", "-level", "4.0",
        "-pix_fmt", "yuv420p",
        "-crf", "$Crf", "-preset", "medium",
        "-movflags", "+faststart"
    )
    if (Test-HasAudio $InputPath) {
        $args += @("-c:a", "aac", "-b:a", "128k")
    } else {
        $args += "-an"
    }
    $args += $OutputPath

    & ffmpeg @args
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed: $InputPath" }
}

if (-not (Test-Path $Manifest)) { throw "Manifest not found: $Manifest" }
if (-not (Test-Path $BackupRoot)) { throw "Backup not found: $BackupRoot" }

$rows = Import-Csv -Path $Manifest -Encoding UTF8
if ($Category -ne "all") {
    $rows = $rows | Where-Object { $_.Category -eq $Category }
}

$results = @()
$totalOrig = 0L
$totalNew  = 0L
$i = 0
$count = @($rows).Count

foreach ($row in $rows) {
    $i++
    $rel = $row.RelativePath -replace '/', '\'
    $src = Join-Path $BackupRoot $rel
    $dstOpt = Join-Path $OptimizedRoot $rel
    $dstAsset = Join-Path $AssetsRoot $rel
    $crf = Get-CrfForFile -Category $row.Category -RelativePath $row.RelativePath -FileName $row.FileName -SizeMB $row.SizeMB

    if (-not (Test-Path -LiteralPath $src)) {
        Write-Warning "[$i/$count] SKIP missing backup: $rel"
        continue
    }

    $origSize = (Get-Item -LiteralPath $src).Length
    $totalOrig += $origSize

    Write-Host "[$i/$count] CRF=$crf $($row.FileName) ($([math]::Round($origSize/1MB,2)) MB)"

    if ($DryRun) { continue }

    Invoke-Reencode -InputPath $src -OutputPath $dstOpt -Crf $crf
    $newSize = (Get-Item -LiteralPath $dstOpt).Length
    $totalNew += $newSize

    $assetDir = Split-Path $dstAsset -Parent
    if (-not (Test-Path $assetDir)) { New-Item -ItemType Directory -Force -Path $assetDir | Out-Null }
    Copy-Item -LiteralPath $dstOpt -Destination $dstAsset -Force

    $pct = if ($origSize -gt 0) { [math]::Round((1 - $newSize / $origSize) * 100, 1) } else { 0 }
    $results += [pscustomobject]@{
        No           = $row.No
        Category     = $row.Category
        RelativePath = $row.RelativePath
        Crf          = $crf
        OrigMB       = [math]::Round($origSize / 1MB, 2)
        NewMB        = [math]::Round($newSize / 1MB, 2)
        SavedPct     = $pct
    }
    Write-Host "  -> $([math]::Round($newSize/1MB,2)) MB (-$pct%)"
}

if (-not $DryRun -and $results.Count -gt 0) {
    $results | Export-Csv -Path $LogFile -NoTypeInformation -Encoding UTF8
    $savedPct = if ($totalOrig -gt 0) { [math]::Round((1 - $totalNew / $totalOrig) * 100, 1) } else { 0 }
    Write-Host ""
    Write-Host "DONE: $($results.Count) files"
    Write-Host "Original: $([math]::Round($totalOrig/1MB,2)) MB -> New: $([math]::Round($totalNew/1MB,2)) MB (-$savedPct%)"
    Write-Host "Log: $LogFile"
}
