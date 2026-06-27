# Restore original intro MP4 files from MBody_Revised into MBody_Revised_Clean.

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path $PSScriptRoot -Parent

$src = Join-Path $projectRoot '..\MBody_Revised\Assets\Shader'

$dst = Join-Path $projectRoot 'Assets\Shader'



if (-not (Test-Path -LiteralPath $src)) {

    throw "Source folder missing: $src"

}



New-Item -ItemType Directory -Force -Path $dst | Out-Null



Write-Host '=== Restore intro MP4 originals ==='

$files = Get-ChildItem -LiteralPath $src -Filter '*.mp4' -File

if ($files.Count -eq 0) {

    throw "No MP4 files found in $src"

}



foreach ($file in $files) {

    $to = Join-Path $dst $file.Name

    Copy-Item -LiteralPath $file.FullName -Destination $to -Force



    $metaFrom = "$($file.FullName).meta"

    $metaTo = "$to.meta"

    if (Test-Path -LiteralPath $metaFrom) {

        Copy-Item -LiteralPath $metaFrom -Destination $metaTo -Force

    }



    $mb = [math]::Round((Get-Item -LiteralPath $to).Length / 1MB, 2)

    Write-Host "OK $($file.Name) ($mb MB)"

}



Write-Host 'Done. Reimport in Unity or rebuild Android APK.'

