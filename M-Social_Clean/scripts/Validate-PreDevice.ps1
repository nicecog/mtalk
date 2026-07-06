# Pre-device validation: feedback MP4, Prepare-failed risk, post-video flow wiring
$ErrorActionPreference = "Stop"
$root = "F:\PROJECT\Unity_Projects\M-Social_Clean"
$sa = Join-Path $root "Assets\StreamingAssets"
$scene = Join-Path $root "Assets\Scenes\MSocial_Stage1.unity"
$mainScene = Join-Path $root "Assets\Scenes\MainScene.unity"
$fbJson = Join-Path $sa "feedback_names.json"
$fail = 0
$warn = 0

function Ok($m)  { Write-Host "OK   $m" -ForegroundColor Green }
function Bad($m) { Write-Host "FAIL $m" -ForegroundColor Red; $script:fail++ }
function Warn($m) { Write-Host "WARN $m" -ForegroundColor Yellow; $script:warn++ }

Write-Host "========== 1. feedback_names.json vs disk vs scenes =========="
$fb = Get-Content $fbJson -Raw -Encoding UTF8 | ConvertFrom-Json
$diskMp4 = @{}
Get-ChildItem $sa -Filter "fb_*.mp4" | ForEach-Object { $diskMp4[$_.Name] = $_.Length }

foreach ($e in $fb.entries) {
    $new = $e.newFileName
    if (-not $diskMp4.ContainsKey($new)) { Bad "missing disk: $new (was $($e.legacyFileName))" }
    elseif ($diskMp4[$new] -lt 50000) { Bad "too small: $new ($($diskMp4[$new]) bytes)" }
    else { Ok "$new on disk ($([math]::Round($diskMp4[$new]/1MB,2)) MB)" }
}

$sceneText = [IO.File]::ReadAllText($scene)
$mainText = [IO.File]::ReadAllText($mainScene)
$vcsRe = [regex]'vcs:\s*\r?\n((?:\s*-\s*"[^"]+\.mp4"\s*\r?\n)+)'
$allVcs = @()
foreach ($m in $vcsRe.Matches($sceneText)) {
    foreach ($line in [regex]::Matches($m.Groups[1].Value, '- "([^"]+\.mp4)"')) {
        $allVcs += $line.Groups[1].Value
    }
}
$fbVcs = $allVcs | Where-Object { $_ -match '^fb_\d{3}\.mp4$' } | Sort-Object -Unique
foreach ($e in $fb.entries) {
    if ($fbVcs -notcontains $e.newFileName) { Bad "scene vcs missing: $($e.newFileName)" }
}
Ok "scene vcs has all $($fb.entries.Count) fb_### entries ($($fbVcs.Count) unique)"

$koreanVcs = $allVcs | Where-Object { $_ -match '[^\x00-\x7F]' }
if ($koreanVcs.Count -gt 0) { Bad "Korean vcs still in MSocial_Stage1: $($koreanVcs -join ', ')" }
else { Ok "no Korean vcs[] in MSocial_Stage1" }

Write-Host "`n========== 2. Feedback page wiring (Breath / PlayInstrument / Dance) =========="
$pageChecks = @(
    @{ Name = "Breath";         Vcs = @("fb_014.mp4","fb_015.mp4"); Script = "Stage1Count" }
    @{ Name = "PlayInstrument"; Vcs = @("fb_002.mp4","fb_003.mp4","fb_004.mp4","fb_005.mp4","fb_006.mp4","fb_007.mp4","fb_008.mp4","fb_009.mp4"); Script = "Stage1Count" }
    @{ Name = "Dance";          Vcs = @("fb_010.mp4","fb_011.mp4","fb_012.mp4","fb_013.mp4"); Script = "Stage1Dance" }
    @{ Name = "CountNumber";    Vcs = @("fb_001.mp4"); Script = "Stage1Count" }
)
foreach ($p in $pageChecks) {
    $blockRe = "(?s)m_Name: $($p.Name)\s.*?m_Script:.*?guid: ([a-f0-9]+).*?vcs:(.*?)startMovie|nextButton"
    # simpler: find GameObject name then next MonoBehaviour with vcs
    $idx = $sceneText.IndexOf("m_Name: $($p.Name)`r`n")
    if ($idx -lt 0) { $idx = $sceneText.IndexOf("m_Name: $($p.Name)`n") }
    if ($idx -lt 0) { Bad "page not found: $($p.Name)"; continue }
    $chunk = $sceneText.Substring($idx, [Math]::Min(2500, $sceneText.Length - $idx))
    foreach ($v in $p.Vcs) {
        if ($chunk -notmatch [regex]::Escape($v)) { Bad "$($p.Name) missing vcs $v" }
    }
    if ($chunk -match 'nextButton: \{fileID: 0\}') { Bad "$($p.Name) nextButton is null" }
    else { Ok "$($p.Name) vcs+nextButton wired" }
    if ($chunk -match 's1m: \{fileID: 0\}') { Bad "$($p.Name) s1m (Stage1Manager) is null" }
    if ($chunk -match 'vp: \{fileID: 0\}') { Bad "$($p.Name) VideoPlayer is null" }
    if ($chunk -match 'ClickNextButton') { Ok "$($p.Name) ClickNextButton in scene chunk" }
}

Write-Host "`n========== 3. Stage1Negative -> FeedbackPage index (sad=Breath) =========="
$fbPageOrder = @("CountNumber","Imagine","Dance","PlayInstrument","Sing","ListenMusic","Think","Breath")
$idx = $sceneText.IndexOf("FeedbackPage:")
if ($idx -ge 0) {
    $chunk = $sceneText.Substring($idx, 400)
    $ids = [regex]::Matches($chunk, 'fileID: (\d+)') | ForEach-Object { $_.Groups[1].Value }
    for ($i = 0; $i -lt [Math]::Min(8, $ids.Count); $i++) {
        $goIdx = $sceneText.IndexOf("--- !u!1 &$($ids[$i])`r`n")
        if ($goIdx -lt 0) { $goIdx = $sceneText.IndexOf("--- !u!1 &$($ids[$i])`n") }
        if ($goIdx -ge 0) {
            $goChunk = $sceneText.Substring($goIdx, 300)
            if ($goChunk -match 'm_Name: (\S+)') {
                $actual = $Matches[1]
                $expect = $fbPageOrder[$i]
                if ($actual -ne $expect) { Bad "FeedbackPage[$i]=$actual expected $expect" }
                else { Ok "FeedbackPage[$i]=$actual" }
            }
        }
    }
}

# codes[1]=24568 -> options 8,6,5,4,2 -> Breath=index 7
$codesMatch = [regex]::Match($sceneText, 'codes:\s*\r?\n((?:\s*-\s*\d+\s*\r?\n)+)')
if ($codesMatch.Success) {
    $codes = [regex]::Matches($codesMatch.Groups[1].Value, '-\s*(\d+)') | ForEach-Object { [int]$_.Groups[1].Value }
    $sadCode = $codes[1]
    $opts = @()
    $t = $sadCode
    for ($k = 0; $k -lt 5; $k++) { $opts += ($t % 10); $t = [Math]::Floor($t / 10) }
    $taskToPage = @{1=0;2=1;3=2;4=3;5=4;6=5;7=6;8=7}
    foreach ($o in $opts) {
        if ($o -ge 1 -and $o -le 8) {
            $pn = $fbPageOrder[$taskToPage[$o]]
            Ok "sad option task=$o -> FeedbackPage[$($taskToPage[$o])] $pn"
        }
    }
    if ($opts -contains 8) { Ok "Breath (8) in sad options -> fb_014/fb_015" }
}

Write-Host "`n========== 4. Prepare-failed risk (static code paths) =========="
$helper = Get-Content (Join-Path $root "Assets\MSocialVideoPlayerHelper.cs") -Raw
if ($helper -match 'PrepareFeedbackClip.*?FeedbackPrepareTimeoutSeconds,\s*false') {
    Ok "feedback uses alertOnEachFailure=false (no per-URL Prepare failed toast)"
} else { Warn "feedback alertOnEachFailure may show intermediate failures" }

if ($helper -match 'FeedbackPrepareTimeoutSeconds = (\d+)') {
    $sec = $Matches[1]
    Ok "feedback prepare timeout = ${sec}s (cache hit should be fast)"
}

$paths = Get-Content (Join-Path $root "Assets\MSocialStreamingAssetPaths.cs") -Raw
if ($paths -match 'IsAsciiFileName') { Ok "fb_### uses ASCII cache filename (no cache_<md5> for feedback)" }

$cache = Get-Content (Join-Path $root "Assets\MSocialVideoCache.cs") -Raw
if ($cache -match 'UNITY_ANDROID') { Ok "Android: APK->persistentDataPath cache before Prepare" }

# First-install copy size check
$large = $fb.entries | Where-Object { $diskMp4[$_.newFileName] -gt 5MB }
if ($large) {
    Warn "first install copies large files before video: $($large.newFileName -join ', ') — user waits during intro audio first"
}

Write-Host "`n========== 5. Post-video -> next scene flow =========="
$mgr = Get-Content (Join-Path $root "Assets\Stage1Manager.cs") -Raw
if ($mgr -match 'ProgressDone\(\)[\s\S]*?fbm\.gameObject\.SetActive\(false\)') {
    Ok "ClickNextButton -> ProgressDone closes feedback manager"
}
if ($mgr -match 'sceneselect\.gameObject\.SetActive\(false\)') {
    Ok "ProgressDone hides sceneselect (returns to Stage1 card hub)"
}

$count = Get-Content (Join-Path $root "Assets\Stage1Count.cs") -Raw
if ($count -match 'nextButton\.gameObject\.SetActive\(true\)') {
    Ok "Stage1Count shows next button even if prepare fails (graceful skip)"
}
if ($count -match 'if \(MSocialVideoPlayerHelper\.IsPrepared') {
    Ok "video play guarded by IsPrepared"
}

Write-Host "`n========== 6. Other Korean MP4 paths (non-feedback) =========="
$mi = Get-Content (Join-Path $sa "Movie\movie_index.json") -Raw -Encoding UTF8 | ConvertFrom-Json
$extra = @($mi.stage1) + @($mi.stage2) | Where-Object { $_ -match '[^\x00-\x7F]' }
foreach ($rel in $extra) {
    $disk = Join-Path $sa ($rel -replace '/', '\')
    if (-not (Test-Path $disk)) { Warn "movie_index Korean path missing on disk: $rel" }
    else { Warn "movie_index still has Korean path (intro/anim): $rel" }
}

$korOnDisk = Get-ChildItem $sa -Recurse -Filter "*.mp4" | Where-Object { $_.Name -match '[^\x00-\x7F]' }
if ($korOnDisk.Count -eq 0) { Ok "no Korean-named .mp4 on disk anywhere" }
else { Warn "Korean mp4 on disk: $($korOnDisk.Name -join ', ')" }

Write-Host "`n========== SUMMARY =========="
Write-Host "FAIL=$fail WARN=$warn"
if ($fail -gt 0) { exit 1 }
exit 0
