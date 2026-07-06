# MBody.unity SceneFlow / ButtonFlow / onClick wiring validation
$ErrorActionPreference = "Stop"

$root = if ($PSScriptRoot) { Split-Path $PSScriptRoot -Parent } else { Get-Location }
$py = Join-Path $root "scripts\validate-scene-flow.py"
$scene = Join-Path $root "Assets\Scenes\MBody.unity"

if (-not (Test-Path $scene)) { throw "Scene not found: $scene" }
if (-not (Test-Path $py)) { throw "Validator not found: $py" }

Write-Host "========== MBody Scene Flow Validation ==========" -ForegroundColor Cyan
Write-Host "Scene: $scene"

$exitCode = 0
python $py $root
if ($LASTEXITCODE -ne 0) { $exitCode = $LASTEXITCODE }

if ($exitCode -eq 0) {
    Write-Host "`nPASS: scene flow validation" -ForegroundColor Green
} else {
    Write-Host "`nFAIL: scene flow validation (exit $exitCode)" -ForegroundColor Red
}

exit $exitCode
