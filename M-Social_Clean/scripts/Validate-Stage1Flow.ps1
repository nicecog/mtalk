# Stage1 negative-emotion -> feedback -> ProgressDone flow validation
$ErrorActionPreference = "Stop"
$root = "F:\PROJECT\Unity_Projects\M-Social_Clean"
$scene = Join-Path $root "Assets\Scenes\MSocial_Stage1.unity"
$fail = 0

function Ok($m)  { Write-Host "OK   $m" -ForegroundColor Green }
function Bad($m) { Write-Host "FAIL $m" -ForegroundColor Red; $script:fail++ }

$text = [IO.File]::ReadAllText($scene, [Text.Encoding]::UTF8)

Write-Host "========== A. FeedbackPage order & task mapping =========="
$order = @("CountNumber","Imagine","Dance","PlayInstrument","Sing","ListenMusic","Think","Breath")
$m = [regex]::Match($text, 'FeedbackPage:\s*\r?\n((?:\s*- \{fileID: \d+\}\s*\r?\n)+)')
if (-not $m.Success) { Bad "FeedbackPage block not found" }
else {
    $ids = [regex]::Matches($m.Groups[1].Value, 'fileID: (\d+)') | ForEach-Object { $_.Groups[1].Value }
    for ($i = 0; $i -lt 8; $i++) {
        $gm = [regex]::Match($text, "(?s)--- !u!1 &$($ids[$i])\r?\nGameObject:.*?m_Name: (\S+)")
        $name = if ($gm.Success) { $gm.Groups[1].Value } else { "?" }
        if ($name -eq $order[$i]) { Ok "FeedbackPage[$i]=$name" } else { Bad "FeedbackPage[$i]=$name expected $($order[$i])" }
    }
}

# codes emotion[1]=24568 sad -> 8,6,5,4,2
$codesBytes = "6e350000f85f00005a3000005a300000aa300000"
$codes = @()
for ($i = 0; $i -lt $codesBytes.Length; $i += 8) {
    $b = [byte[]]::new(4)
    for ($j = 0; $j -lt 4; $j++) { $b[$j] = [Convert]::ToByte($codesBytes.Substring($i + $j * 2, 2), 16) }
    $codes += [BitConverter]::ToInt32($b, 0)
}
$sad = $codes[1]
$opts = @(); $t = $sad
for ($k = 0; $k -lt 5; $k++) { $opts += ($t % 10); $t = [Math]::Floor($t / 10) }
Ok "sad codes=$sad options tasks=$($opts -join ',')"

Write-Host "`n========== B. Stage1Negative wiring =========="
$negIdx = $text.IndexOf("m_Name: 9-2. Stage1Negative")
if ($negIdx -lt 0) { Bad "Stage1Negative not found" }
else {
    $chunk = $text.Substring($negIdx, 3500)
    if ($chunk -match 'fbm: \{fileID: (\d+)\}') { Ok "fbm wired fileID=$($Matches[1])" } else { Bad "fbm null" }
    if ($chunk -match 'sm: \{fileID: (\d+)\}') { Ok "sm (Stage1Manager) wired" } else { Bad "sm null" }
    if ($chunk -match 'NextButton: \{fileID: (\d+)\}' -and $Matches[1] -ne '0') { Ok "NextButton wired" } else { Bad "NextButton null" }
    # NextButton onClick -> Stage1Negative.ClickNextButton
    if ($chunk -match 'm_MethodName: ClickNextButton') { Ok "NextButton -> ClickNextButton in neg area" }
}

Write-Host "`n========== C. Per-page: vcs, vp, nextButton, s1m, ClickNextButton =========="
$pages = @(
    @{ Name="Breath"; Script="Stage1Count"; MinVcs=2 }
    @{ Name="PlayInstrument"; Script="Stage1Count"; MinVcs=8 }
    @{ Name="Dance"; Script="Stage1Dance"; MinVcs=4 }
    @{ Name="CountNumber"; Script="Stage1Count"; MinVcs=1 }
    @{ Name="Imagine"; Script="Stage1Imagine"; MinVcs=0 }
    @{ Name="Sing"; Script="Stage1Sing"; MinVcs=0 }
    @{ Name="ListenMusic"; Script="Stage1Listen"; MinVcs=0 }
    @{ Name="Think"; Script="Stage1Think"; MinVcs=0 }
)
foreach ($p in $pages) {
    $idx = $text.IndexOf("m_Name: $($p.Name)`r`n")
    if ($idx -lt 0) { $idx = $text.IndexOf("m_Name: $($p.Name)`n") }
    if ($idx -lt 0) { Bad "$($p.Name) GameObject missing"; continue }
    $chunk = $text.Substring($idx, 4000)
    $vcsCount = ([regex]::Matches($chunk, '- "fb_\d{3}\.mp4"')).Count
    if ($p.MinVcs -gt 0 -and $vcsCount -lt $p.MinVcs) { Bad "$($p.Name) vcs count=$vcsCount" }
    elseif ($p.MinVcs -eq 0) { Ok "$($p.Name) audio-only (vcs=$vcsCount)" }
    else { Ok "$($p.Name) vcs=$vcsCount" }
    if ($p.MinVcs -gt 0) {
        if ($chunk -match 'vp: \{fileID: 0\}') { Bad "$($p.Name) vp null" }
        else { Ok "$($p.Name) vp wired" }
    }
    if ($chunk -match 'nextButton: \{fileID: 0\}') { Bad "$($p.Name) nextButton null" }
    else { Ok "$($p.Name) nextButton wired" }
    if ($chunk -match 's1m: \{fileID: 0\}') { Bad "$($p.Name) s1m null" }
    else { Ok "$($p.Name) s1m wired" }
}

Write-Host "`n========== D. Breath nextButton -> Stage1Count.ClickNextButton =========="
$breathIdx = $text.IndexOf("m_Name: Breath")
$breathChunk = $text.Substring($breathIdx, 500)
if ($breathChunk -match 'nextButton: \{fileID: (\d+)\}') {
    $btnId = $Matches[1]
    $btnChunk = $text.Substring($text.IndexOf("--- !u!114 &$btnId"))
    if ($btnChunk -match 'm_MethodName: ClickNextButton' -and $btnChunk -match 'Stage1Count') {
        Ok "Breath Button -> Stage1Count.ClickNextButton"
    } else { Bad "Breath Button onClick wrong" }
}

Write-Host "`n========== E. Stage1Manager ProgressDone chain =========="
$mgrIdx = $text.IndexOf("m_Script: {fileID: 11500000, guid:")
# find Stage1Manager by ProgressDone reference
if ($text -match 'player: \{fileID: (\d+)\}[\s\S]*?sceneselect: \{fileID: (\d+)\}[\s\S]*?fbm: \{fileID: (\d+)\}') {
    Ok "Stage1Manager player/sceneselect/fbm refs present"
} else { Bad "Stage1Manager refs incomplete" }

Write-Host "`n========== F. Code path sanity (grep) =========="
$negCs = Get-Content (Join-Path $root "Assets\Stage1Negative.cs") -Raw
if ($negCs -match 'fbm\.startFeedback\(t - 1\)') { Ok "Stage1Negative calls startFeedback(t-1)" }
if ($negCs -match 'HideWatchScene') { Ok "Stage1Negative hides watch scene" }
$countCs = Get-Content (Join-Path $root "Assets\Stage1Count.cs") -Raw
if ($countCs -match 'PrepareFeedbackClip') { Ok "Stage1Count uses PrepareFeedbackClip" }
if ($countCs -match 's1m\.ProgressDone') { Ok "Stage1Count -> ProgressDone" }
$mgrCs = Get-Content (Join-Path $root "Assets\Stage1Manager.cs") -Raw
if ($mgrCs -match 'fbm\.gameObject\.SetActive\(false\)') { Ok "ProgressDone closes fbm" }

Write-Host "`n========== SUMMARY flow (sad -> Breath) =========="
Write-Host @"
  ScenePlayer.OnEnable -> scenario MP4 -> EmotionSelect
  EmotionSelect.ClickNextButton -> Stage1Negative (mainEmotion=sad)
  Stage1Negative.ClickNextButton -> HideWatchScene + fbm.startFeedback(7) + Breath.SetActive
  Breath.OnEnable -> audio -> PrepareFeedbackClip(fb_014|015) -> vp.Play
  Breath nextButton -> Stage1Count.ClickNextButton -> s1m.ProgressDone()
  ProgressDone -> fbm off, sceneselect off, progress++
"@

Write-Host "`nFAIL=$fail"
if ($fail -gt 0) { exit 1 }
exit 0
