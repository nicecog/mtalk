# Play Mode smoke check via Unity MCP (requires Unity Editor + MCP For Unity connected)
param(
    [int]$WarmupSec = 12,
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

if (-not $ProjectRoot) {
    $ProjectRoot = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { Get-Location }
}

Write-Host "========== MBody Play Mode Flow (MCP) ==========" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"
Write-Host "Warmup:  ${WarmupSec}s (IntroLogo TimerFlow -> MBodyLogin)"

# Step 0: static scene wiring (no Unity required)
$validate = Join-Path $ProjectRoot "scripts\Validate-SceneFlow.ps1"
if (Test-Path $validate) {
    & $validate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# MCP calls require Cursor agent / mcp-cli — this script documents manual checks when MCP unavailable.
Write-Host "`n--- Unity MCP steps (run from Cursor agent when Editor is connected) ---" -ForegroundColor Yellow
Write-Host @"
1. refresh_unity (compile=request, wait_for_ready=true)
2. read_console action=clear
3. manage_editor action=play
4. Wait ${WarmupSec}s
5. read_console types=[error,warning,log] — expect [FlowDiag] and JsonRequest base URL
6. find_gameobjects search_method=by_tag search_term=SceneFlow include_inactive=true
7. manage_editor action=stop
"@

Write-Host "`nExpected Play Mode sequence (approx):" -ForegroundColor Cyan
Write-Host "  t=0s   IntroLogo (FirstPage)"
Write-Host "  t=~?s  MBodyLogin (TimerFlow on IntroLogo)"
Write-Host "  manual: login ID -> BodyDanceSelect -> Body -> MusicSelect ..."

Write-Host "`nNote: Re-run this check from Cursor with Unity MCP connected for live console capture." -ForegroundColor DarkYellow
exit 0
