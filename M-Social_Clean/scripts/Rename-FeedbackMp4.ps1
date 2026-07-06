#Requires -Version 5.1
<#
.SYNOPSIS
  Rename StreamingAssets feedback MP4s to fb_XXX.mp4 and update scene vcs[] references.
  Writes Assets/StreamingAssets/feedback_names.json (legacy Korean -> ASCII id).
#>
param(
    [string]$ProjectRoot = "F:\PROJECT\Unity_Projects\M-Social_Clean",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$py = Join-Path $ProjectRoot "scripts\rename_feedback_mp4.py"
$dryFlag = if ($DryRun) { "1" } else { "0" }
python $py $ProjectRoot $dryFlag
