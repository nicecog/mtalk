# Rename Stage1/Stage2 scenario MP4 files (Korean filename -> ASCII+number ids)
# and update Assets/StreamingAssets/Movie/movie_index.json accordingly.
#
# Goal
# - Android VideoPlayer jar/file path + Korean filenames 문제 회피를 위해,
#   Stage1/Stage2 시나리오 영상 파일명을 s1_### / s2_### 형태로 변경합니다.
# - 호출 경로는 movie_index.json의 stage1/stage2 값을 새 파일 경로로 매핑합니다.
# - 매핑 정보는 movie_names.json에 legacy(원래 한글) -> new(id) 형태로 저장합니다.
#
# Usage
#   powershell -NoProfile -ExecutionPolicy Bypass -File "...\Rename-MsocialStage1Stage2MoviesAscii.ps1"
#   powershell ... -Apply   (실제로 Move + JSON 갱신)
#

param(
    [switch]$Apply
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$StreamingAssetsRoot = Join-Path $ProjectRoot "Assets\\StreamingAssets"
$MovieDir = Join-Path $StreamingAssetsRoot "Movie"
$MovieIndexPath = Join-Path $MovieDir "movie_index.json"
$NamesPath = Join-Path $MovieDir "movie_names.json"
$ReportPath = Join-Path $ProjectRoot "Build\\Android\\movie-rename-stage1-stage2-report.txt"

if (-not (Test-Path -LiteralPath $MovieIndexPath)) {
    throw "movie_index.json not found: $MovieIndexPath"
}

function Read-JsonUtf8([string]$path) {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $text = [System.IO.File]::ReadAllText($path, $utf8)
    return $text | ConvertFrom-Json
}

function Write-JsonUtf8([string]$path, $obj) {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $json = $obj | ConvertTo-Json -Depth 50
    [System.IO.File]::WriteAllText($path, $json, $utf8)
}

function FixStreamingAssetsRelPath([string]$rel) {
    # 현재 movie_index.json은 "Movie\\Stage1\\..." 형태이지만,
    # 실제 StreamingAssets 파일은 "Movie\\Movie\\Stage1\\..." 아래에 존재합니다.
    # 따라서 "Movie\\" 앞에 한 번 더 "Movie\\"를 삽입합니다.
    if ($rel -match '^Movie\\') {
        return $rel -replace '^Movie\\', 'Movie\\Movie\\'
    }
    if ($rel -match '^Movie/') {
        return $rel -replace '^Movie/', 'Movie/Movie/'
    }
    return $rel
}

function ReplaceFileName([string]$rel, [string]$newFileName) {
    $dir = Split-Path $rel -Parent
    if ([string]::IsNullOrEmpty($dir)) {
        return $newFileName
    }
    return ($dir.TrimEnd('\','/') + "\" + $newFileName)
}

function MoveOrCopy([string]$from, [string]$to) {
    if (-not (Test-Path -LiteralPath $from)) {
        return $false
    }
    $toDir = Split-Path $to -Parent
    if (-not (Test-Path -LiteralPath $toDir)) {
        if ($Apply) {
            New-Item -ItemType Directory -Path $toDir -Force | Out-Null
        }
    }
    if (Test-Path -LiteralPath $to) {
        if ($Apply) {
            Remove-Item -LiteralPath $to -Force
        }
    }
    if ($Apply) {
        Move-Item -LiteralPath $from -Destination $to -Force
    }
    return $true
}

$oldIndex = Read-JsonUtf8 $MovieIndexPath

if ($null -eq $oldIndex.stage1 -or $oldIndex.stage1.Count -ne 32) {
    throw "movie_index.stage1 missing or not 32 items."
}
if ($null -eq $oldIndex.stage2 -or $oldIndex.stage2.Count -ne 32) {
    throw "movie_index.stage2 missing or not 32 items."
}

$names = $null
if (Test-Path -LiteralPath $NamesPath) {
    try {
        $names = Read-JsonUtf8 $NamesPath
    } catch {
        $names = $null
    }
}
if ($null -eq $names) {
    $names = [ordered]@{
        version = 1
        stage1 = @()
        stage2 = @()
    }
} else {
    if ($null -eq $names.stage1) { $names.stage1 = @() }
    if ($null -eq $names.stage2) { $names.stage2 = @() }
}

$newIndex = [ordered]@{
    stage1 = @()
    stage2 = @()
    intro  = $oldIndex.intro
}

$log = New-Object System.Collections.Generic.List[string]

# stage1
$newStage1Arr = New-Object System.Collections.Generic.List[string]
$namesStage1Arr = New-Object System.Collections.Generic.List[object]
for ($i = 0; $i -lt 32; $i++) {
    $oldRel = $oldIndex.stage1[$i]
    $legacyFileName = [System.IO.Path]::GetFileName($oldRel)
    $id = ("s1_{0:D3}" -f ($i + 1))
    $newFileName = "$id.mp4"
    $actualRel = FixStreamingAssetsRelPath $oldRel
    $newRel = ReplaceFileName $actualRel $newFileName

    $fromFull = Join-Path $StreamingAssetsRoot ($actualRel -replace '/', '\')
    $toFull = Join-Path $StreamingAssetsRoot ($newRel -replace '/', '\')

    $ok = MoveOrCopy $fromFull $toFull
    if ($ok) { $log.Add("OK [stage1][$i] $legacyFileName -> $newFileName") }
    else { $log.Add("MISSING [stage1][$i] $legacyFileName from=$fromFull") }

    $newIndex.stage1 += , ($newRel -replace '/', '\')
    $names.stage1 += , ([ordered]@{
        index = $i
        id = $id
        legacyFileName = $legacyFileName
        legacyRel = ($oldRel -replace '/', '\')
        actualRelBeforeRename = ($actualRel -replace '/', '\')
        newRel = ($newRel -replace '/', '\')
    })
}

# stage2
for ($i = 0; $i -lt 32; $i++) {
    $oldRel = $oldIndex.stage2[$i]
    $legacyFileName = [System.IO.Path]::GetFileName($oldRel)
    $id = ("s2_{0:D3}" -f ($i + 1))
    $newFileName = "$id.mp4"
    $actualRel = FixStreamingAssetsRelPath $oldRel
    $newRel = ReplaceFileName $actualRel $newFileName

    $fromFull = Join-Path $StreamingAssetsRoot ($actualRel -replace '/', '\')
    $toFull = Join-Path $StreamingAssetsRoot ($newRel -replace '/', '\')

    $ok = MoveOrCopy $fromFull $toFull
    if ($ok) { $log.Add("OK [stage2][$i] $legacyFileName -> $newFileName") }
    else { $log.Add("MISSING [stage2][$i] $legacyFileName from=$fromFull") }

    $newIndex.stage2 += , ($newRel -replace '/', '\')
    $names.stage2 += , ([ordered]@{
        index = $i
        id = $id
        legacyFileName = $legacyFileName
        legacyRel = ($oldRel -replace '/', '\')
        actualRelBeforeRename = ($actualRel -replace '/', '\')
        newRel = ($newRel -replace '/', '\')
    })
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add("Apply=$Apply")
$report.Add("Updated: stage1 and stage2 file names + movie_index paths (Movie/Movie prefix fix).")
$report.Add("")
$report.AddRange($log)

$reportDir = Split-Path $ReportPath -Parent
if ($Apply -and -not (Test-Path -LiteralPath $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
}

if ($Apply) {
    Write-JsonUtf8 $MovieIndexPath $newIndex
    Write-JsonUtf8 $NamesPath $names
}

[System.IO.File]::WriteAllLines($ReportPath, $report, (New-Object System.Text.UTF8Encoding($false)))

Write-Host "=== Rename stage1/stage2 ASCII ==="
Write-Host "Apply=$Apply"
Write-Host "Report=$ReportPath"
if ($Apply) {
    Write-Host "movie_index.json updated."
    Write-Host "movie_names.json updated."
} else {
    Write-Host "Dry-run only. Re-run with -Apply to commit rename."
}

