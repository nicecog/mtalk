$backup = 'F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Build\DanceVideoBackup'
$unity = 'F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Assets\DanceVideo'
$encoded = 'F:\PROJECT\Unity_Projects\MBody_Revised_Clean\Build\ReencodedDance'

$origSum = (Get-ChildItem -LiteralPath $backup -Filter '*.mp4' -Recurse | Measure-Object -Property Length -Sum).Sum
$newSum = (Get-ChildItem -LiteralPath $unity -Filter '*.mp4' -Recurse | Measure-Object -Property Length -Sum).Sum
$encCount = (Get-ChildItem -LiteralPath $encoded -Filter '*_720p.mp4' -Recurse).Count
$unityCount = (Get-ChildItem -LiteralPath $unity -Filter '*.mp4' -Recurse).Count

Write-Host "Unity MP4 count:    $unityCount"
Write-Host "Reencoded count:    $encCount"
Write-Host "Backup total:       $([math]::Round($origSum/1GB, 2)) GB"
Write-Host "Unity total:        $([math]::Round($newSum/1GB, 2)) GB"
Write-Host "Size reduction:     $([math]::Round(($origSum - $newSum)/1GB, 2)) GB ($([math]::Round(100*(1-$newSum/$origSum),0))%)"

$tiny = Get-ChildItem -LiteralPath $unity -Filter '*.mp4' -Recurse | Where-Object { $_.Length -lt 100000 }
if ($tiny) {
    Write-Warning 'Tiny/corrupt files:'
    $tiny | ForEach-Object { Write-Warning "$($_.FullName) ($($_.Length) bytes)" }
} else {
    Write-Host 'All Unity MP4 files >= 100 KB'
}
