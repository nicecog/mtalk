# Verify all Unity DanceVideo MP4s are valid (moov atom present via ffprobe).
param(
    [string]$Root = (Join-Path $PSScriptRoot '..\Assets\DanceVideo')
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path $Root).Path

if (-not (Get-Command ffprobe -ErrorAction SilentlyContinue)) {
    Write-Error 'ffprobe not found in PATH.'
}

$files = @(Get-ChildItem -LiteralPath $Root -Filter '*.mp4' -Recurse -File | Sort-Object FullName)
$bad = New-Object System.Collections.Generic.List[string]

foreach ($f in $files) {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 $f.FullName 2>$null | Out-Null
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevEap
    if ($exitCode -ne 0) {
        $bad.Add($f.FullName)
    }
}

Write-Host "Checked: $($files.Count) MP4 files"
if ($bad.Count -eq 0) {
    Write-Host 'PASS: all MP4 files readable by ffprobe' -ForegroundColor Green
    exit 0
}

Write-Host "FAIL: $($bad.Count) corrupt/unreadable files:" -ForegroundColor Red
foreach ($p in $bad) { Write-Host "  $p" }
exit 1
