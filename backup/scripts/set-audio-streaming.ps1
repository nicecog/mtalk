# Set Unity AudioImporter loadType to Streaming (2) for startup RAM reduction.
param(
    [string[]]$Roots = @(
        (Join-Path $PSScriptRoot '..\Assets\BodyMusics'),
        (Join-Path $PSScriptRoot '..\Assets\DanceMusic'),
        (Join-Path $PSScriptRoot '..\Assets\Images\New_210928'),
        (Join-Path $PSScriptRoot '..\Assets\Images\SoundIcons')
    ),
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$changed = 0
$scanned = 0

Write-Host '=== Set Audio Streaming (loadType: 2) ==='

foreach ($root in $Roots) {
    $path = Resolve-Path $root -ErrorAction SilentlyContinue
    if (-not $path) {
        Write-Warning "Skip missing: $root"
        continue
    }
    Write-Host "Scan: $path"
    Get-ChildItem -Path $path -Filter '*.meta' -Recurse -File | ForEach-Object {
        $scanned++
        $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
        if ($text -notmatch 'AudioImporter:') { return }
        if ($text -notmatch 'loadType:\s*0\b') { return }

        $newText = $text -replace 'loadType:\s*0\b', 'loadType: 2'
        $newText = $newText -replace 'preloadAudioData:\s*1\b', 'preloadAudioData: 0'
        if ($newText -eq $text) { return }

        $changed++
        if ($WhatIf) {
            Write-Host "  [whatif] $($_.FullName)"
        } else {
            Set-Content -LiteralPath $_.FullName -Value $newText -Encoding UTF8 -NoNewline
        }
    }
}

Write-Host "Scanned meta: $scanned, changed: $changed"
if ($WhatIf) { Write-Host 'Re-run without -WhatIf to apply.' }
