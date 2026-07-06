# Validates Stage1 feedback vcs[] paths against StreamingAssets MP4 files
$ErrorActionPreference = "Stop"
$root = "F:\PROJECT\Unity_Projects\M-Social_Clean"
$sa = Join-Path $root "Assets\StreamingAssets"
$scene = Join-Path $root "Assets\Scenes\MSocial_Stage1.unity"

$mp4s = @{}
Get-ChildItem $sa -Filter "*.mp4" | ForEach-Object { $mp4s[$_.Name] = $_.Length }

$text = [System.IO.File]::ReadAllText($scene)
$matches = [regex]::Matches($text, '- "((?:\\u[0-9a-fA-F]{4}|[^"])+?\.mp4)"')
$paths = @{}
foreach ($m in $matches) {
    $raw = $m.Groups[1].Value
    $decoded = [regex]::Unescape($raw)
    if (-not $paths.ContainsKey($decoded)) { $paths[$decoded] = 0 }
    $paths[$decoded]++
}

Write-Host "=== Feedback MP4 paths in MSocial_Stage1.unity ==="
$missing = @()
foreach ($p in ($paths.Keys | Sort-Object)) {
    if ($mp4s.ContainsKey($p)) {
        $mb = [math]::Round($mp4s[$p] / 1MB, 2)
        Write-Host "OK   $p ($mb MB, refs=$($paths[$p]))"
    } else {
        Write-Host "MISS $p (refs=$($paths[$p]))"
        $missing += $p
    }
}

Write-Host ""
Write-Host "=== StreamingAssets MP4 not in scene vcs ==="
foreach ($f in ($mp4s.Keys | Sort-Object)) {
    if (-not $paths.ContainsKey($f)) { Write-Host "extra $f" }
}

# codes for sad emotion (index 1)
$codesHex = "6e350000f85f00005a3000005a300000aa300000"
$codes = @()
for ($i = 0; $i -lt $codesHex.Length; $i += 8) {
    $bytes = [byte[]]::new(4)
    for ($j = 0; $j -lt 4; $j++) { $bytes[$j] = [Convert]::ToByte($codesHex.Substring($i + $j * 2, 2), 16) }
    $codes += [BitConverter]::ToInt32($bytes, 0)
}
Write-Host ""
Write-Host "=== Stage1Negative codes (emotion index -> options 1-8) ==="
$names = @{1="Count";2="Imagine";3="Dance";4="PlayInstrument";5="Sing";6="ListenMusic";7="Think";8="Breath"}
for ($ei = 0; $ei -lt $codes.Length; $ei++) {
    $t = $codes[$ei]
    $opts = @()
    for ($k = 0; $k -lt 5; $k++) { $opts += ($t % 10); $t = [math]::Floor($t / 10) }
    $labels = $opts | ForEach-Object { if ($names.ContainsKey($_)) { $names[$_] } else { "?" } }
    Write-Host "emotion[$ei] code=$($codes[$ei]) options: $($labels -join ', ')"
}

if ($missing.Count -gt 0) { exit 1 }
